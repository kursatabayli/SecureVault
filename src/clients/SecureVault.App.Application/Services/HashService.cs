using Konscious.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace SecureVault.App.Application.Services;

public class HashService : IHashService
{
    private readonly ILogger<HashService> _logger;

    public HashService(ILogger<HashService> logger)
    {
        _logger = logger;
    }

    public byte[] GenerateSalt()
    {
        _logger.LogDebug("Generating 16-byte cryptographically secure salt.");
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        return salt;
    }
    public byte[] CreateMasterSecret(string password, byte[] salt)
    {
        _logger.LogDebug("Deriving Master Secret using Argon2id (Iterations: 4, Memory: 64MB).");
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        try
        {
            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = salt,
                DegreeOfParallelism = 2,
                Iterations = 4,
                MemorySize = 65536
            };

            var masterSecret = argon2.GetBytes(32);
            _logger.LogDebug("Master Secret derived successfully.");
            return masterSecret;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "CRITICAL: Argon2id master secret derivation failed.");
            throw;
        }
    }
    public byte[] GetPrivateKeyForAuth(byte[] masterSecret, byte[] salt)
    {
        if (masterSecret == null) throw new InvalidOperationException("Master Secret cannot be null.");

        _logger.LogDebug("Deriving ECDSA Private Key using HKDF-SHA256...");

        var privateKey = HKDF.DeriveKey(
            hashAlgorithmName: HashAlgorithmName.SHA256,
            ikm: masterSecret,
            outputLength: 32,
            salt: salt,
            info: Encoding.UTF8.GetBytes("ecdsa-auth-key-v1")
        );

        _logger.LogDebug("ECDSA Private Key derived successfully.");
        return privateKey;
    }

    public byte[] GetEncryptionKeyForData(byte[] masterSecret, byte[] salt)
    {
        if (masterSecret == null) throw new InvalidOperationException("Master Secret cannot be null.");

        _logger.LogDebug("Deriving AES Encryption Key using HKDF-SHA256...");

        var encryptionKey = HKDF.DeriveKey(
            hashAlgorithmName: HashAlgorithmName.SHA256,
            ikm: masterSecret,
            outputLength: 32,
            salt: salt,
            info: Encoding.UTF8.GetBytes("aes-gcm-data-key-v1")
        );

        _logger.LogDebug("AES Encryption Key derived successfully.");
        return encryptionKey;
    }
}
