using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using System.Security.Cryptography;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin;

public class SecureChannelManager : ISecureChannelManager
{
    private readonly ICryptoService _cryptoService;
    private readonly ILogger<SecureChannelManager> _logger;

    private ECDiffieHellman? _ecdh;
    private byte[]? _sharedSecret;
    private bool _isDisposed;

    private readonly object _lock = new();

    public bool IsSecureChannelEstablished
    {
        get
        {
            lock (_lock)
            {
                return _sharedSecret != null;
            }
        }
    }

    public SecureChannelManager(ICryptoService cryptoService, ILogger<SecureChannelManager> logger)
    {
        _cryptoService = cryptoService;
        _logger = logger;
    }

    public string InitiateKeyExchange()
    {
        lock (_lock)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(SecureChannelManager));

            _ecdh?.Dispose();
            _sharedSecret = null;

            _logger.LogInformation("Initiating new key exchange. Creating ECDH key pair...");
            _ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

            var publicKey = _ecdh.PublicKey.ExportSubjectPublicKeyInfo();
            return Convert.ToBase64String(publicKey);
        }
    }

    public void FinalizeKeyExchange(string counterPartyPublicKeyBase64)
    {
        lock (_lock)
        {
            if (!_isDisposed)
            {
                if (_ecdh == null)
                {
                    throw new InvalidOperationException("Cannot finalize key exchange before initiating it. Call InitiateKeyExchange first.");
                }

                try
                {
                    _logger.LogInformation("Finalizing key exchange...");
                    var counterPartyPublicKeyBytes = Convert.FromBase64String(counterPartyPublicKeyBase64);

                    using (var counterPartyEcdh = ECDiffieHellman.Create())
                    {
                        counterPartyEcdh.ImportSubjectPublicKeyInfo(counterPartyPublicKeyBytes, out _);
                        _sharedSecret = _ecdh.DeriveKeyMaterial(counterPartyEcdh.PublicKey);
                    }

                    _logger.LogInformation("Secure channel established successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to finalize key exchange. The provided public key may be invalid.");
                    _sharedSecret = null;
                    throw;
                }
            }
            else
                throw new ObjectDisposedException(nameof(SecureChannelManager));
        }
    }

    public byte[] Encrypt<T>(T data)
    {
        lock (_lock)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(SecureChannelManager));

            if (!IsSecureChannelEstablished || _sharedSecret == null)
            {
                throw new InvalidOperationException("Secure channel is not established. Cannot encrypt data.");
            }

            _logger.LogDebug("Encrypting data using secure channel (ECDH shared secret)...");
            return _cryptoService.Encrypt(data, _sharedSecret);
        }
    }

    public T Decrypt<T>(byte[] encryptedData)
    {
        lock (_lock)
        {
            if (!_isDisposed)
            {
                if (!IsSecureChannelEstablished || _sharedSecret == null)
                {
                    throw new InvalidOperationException("Secure channel is not established. Cannot decrypt data.");
                }

                _logger.LogDebug("Decrypting data using secure channel (ECDH shared secret)...");
                return _cryptoService.Decrypt<T>(encryptedData, _sharedSecret);
            }

            throw new ObjectDisposedException(nameof(SecureChannelManager));
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;

            _logger.LogInformation("Disposing SecureChannelManager: Clearing ECDH keys and shared secret from memory.");

            _ecdh?.Dispose();
            _ecdh = null;

            if (_sharedSecret != null)
            {
                Array.Clear(_sharedSecret, 0, _sharedSecret.Length);
                _sharedSecret = null;
            }

            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
