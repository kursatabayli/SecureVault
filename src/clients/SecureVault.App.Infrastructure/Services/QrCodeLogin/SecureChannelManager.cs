using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using System.Security.Cryptography;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin
{
    public class SecureChannelManager : ISecureChannelManager
    {
        private readonly ICryptoService _cryptoService;
        private readonly ILogger<SecureChannelManager> _logger;

        private ECDiffieHellman _ecdh;
        private byte[] _sharedSecret;
        private bool _isDisposed;

        public bool IsSecureChannelEstablished => _sharedSecret != null;

        public SecureChannelManager(ICryptoService cryptoService, ILogger<SecureChannelManager> logger)
        {
            _cryptoService = cryptoService;
            _logger = logger;
        }

        public string InitiateKeyExchange()
        {
            _ecdh?.Dispose();

            _logger.LogInformation("Initiating new key exchange. Creating ECDH key pair...");
            _ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

            var publicKey = _ecdh.PublicKey.ExportSubjectPublicKeyInfo();
            return Convert.ToBase64String(publicKey);
        }

        public void FinalizeKeyExchange(string counterPartyPublicKeyBase64)
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

        public byte[] Encrypt<T>(T data)
        {
            if (!IsSecureChannelEstablished)
            {
                throw new InvalidOperationException("Secure channel is not established. Cannot encrypt data.");
            }
            return _cryptoService.Encrypt(data, _sharedSecret);
        }

        public T Decrypt<T>(byte[] encryptedData)
        {
            if (!IsSecureChannelEstablished)
            {
                throw new InvalidOperationException("Secure channel is not established. Cannot decrypt data.");
            }
            return _cryptoService.Decrypt<T>(encryptedData, _sharedSecret);
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _ecdh?.Dispose();

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
