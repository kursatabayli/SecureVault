using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SecureVault.App.Application.Services;

public class AesGcmCryptoService : ICryptoService
{
    private readonly ILogger<AesGcmCryptoService> _logger;

    public AesGcmCryptoService(ILogger<AesGcmCryptoService> logger)
    {
        _logger = logger;
    }

    private const int AesKeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public byte[] Encrypt<T>(T dataToEncrypt, byte[] encryptionKey)
    {
        if (encryptionKey.Length != AesKeySize)
        {
            _logger.LogError("Encryption failed: Invalid key size ({ActualSize} bytes). Key must be {RequiredSize} bytes.", encryptionKey.Length, AesKeySize);
            throw new ArgumentException($"Invalid key size. Key must be {AesKeySize} bytes.", nameof(encryptionKey));
        }

        var plaintextBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dataToEncrypt));
        _logger.LogDebug("Encrypting object of type {Type} with AES-GCM.", typeof(T).Name);

        using var aesGcm = new AesGcm(encryptionKey, TagSize);

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintextBytes.Length];

        RandomNumberGenerator.Fill(nonce);

        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var encryptedData = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, encryptedData, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, encryptedData, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, encryptedData, NonceSize + TagSize, ciphertext.Length);

        _logger.LogDebug("Encryption completed. Nonce/Tag size: {NonceTagSize}, Ciphertext size: {CiphertextSize}", NonceSize + TagSize, ciphertext.Length);

        return encryptedData;
    }

    public T Decrypt<T>(byte[] encryptedData, byte[] encryptionKey)
    {
        if (encryptionKey.Length != AesKeySize)
        {
            _logger.LogError("Decryption failed: Invalid key size ({ActualSize} bytes). Key must be {RequiredSize} bytes.", encryptionKey.Length, AesKeySize);
            throw new ArgumentException($"Invalid key size. Key must be {AesKeySize} bytes.", nameof(encryptionKey));
        }

        if (encryptedData.Length < NonceSize + TagSize)
        {
            _logger.LogError("Decryption failed: Invalid encrypted data format (length too short).");
            throw new CryptographicException("Invalid encrypted data format.");
        }

        var nonce = new ReadOnlySpan<byte>(encryptedData, 0, NonceSize);
        var tag = new ReadOnlySpan<byte>(encryptedData, NonceSize, TagSize);
        var ciphertext = new ReadOnlySpan<byte>(encryptedData, NonceSize + TagSize, encryptedData.Length - (NonceSize + TagSize));

        using var aesGcm = new AesGcm(encryptionKey, TagSize);

        var plaintextBytes = new byte[ciphertext.Length];

        try
        {
            _logger.LogDebug("Decrypting data for type {Type}...", typeof(T).Name);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);
            _logger.LogDebug("Decryption successful.");
        }
        catch (AuthenticationTagMismatchException ex)
        {
            _logger.LogWarning(ex, "Authentication tag mismatch detected! Data tampering or incorrect key used.");
            throw new SecurityException("Data authentication failed. The data may be tampered with or the key is incorrect.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed due to unexpected cryptographic error.");
            throw;
        }

        var jsonString = Encoding.UTF8.GetString(plaintextBytes);
        return JsonSerializer.Deserialize<T>(jsonString)!;
    }

    public byte[] EncryptBytes(byte[] plaintextBytes, byte[] encryptionKey)
    {
        if (encryptionKey.Length != AesKeySize)
        {
            _logger.LogError("Encryption failed: Invalid key size ({ActualSize} bytes). Key must be {RequiredSize} bytes.", encryptionKey.Length, AesKeySize);
            throw new ArgumentException($"Invalid key size. Key must be {AesKeySize} bytes.", nameof(encryptionKey));
        }

        _logger.LogDebug("Encrypting raw bytes (Size: {Size}) with AES-GCM.", plaintextBytes.Length);
        using var aesGcm = new AesGcm(encryptionKey, TagSize);
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintextBytes.Length];

        RandomNumberGenerator.Fill(nonce);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var encryptedData = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, encryptedData, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, encryptedData, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, encryptedData, NonceSize + TagSize, ciphertext.Length);

        return encryptedData;
    }

    public byte[] DecryptBytes(byte[] encryptedData, byte[] encryptionKey)
    {
        if (encryptionKey.Length != AesKeySize)
        {
            _logger.LogError("Decryption failed: Invalid key size ({ActualSize} bytes). Key must be {RequiredSize} bytes.", encryptionKey.Length, AesKeySize);
            throw new ArgumentException($"Invalid key size. Key must be {AesKeySize} bytes.", nameof(encryptionKey));
        }

        if (encryptedData.Length < NonceSize + TagSize)
        {
            _logger.LogError("Decryption failed: Invalid encrypted data format (length too short).");
            throw new CryptographicException("Invalid encrypted data format.");
        }

        var nonce = new ReadOnlySpan<byte>(encryptedData, 0, NonceSize);
        var tag = new ReadOnlySpan<byte>(encryptedData, NonceSize, TagSize);
        var ciphertext = new ReadOnlySpan<byte>(encryptedData, NonceSize + TagSize, encryptedData.Length - (NonceSize + TagSize));

        using var aesGcm = new AesGcm(encryptionKey, TagSize);
        var plaintextBytes = new byte[ciphertext.Length];

        try
        {
            _logger.LogDebug("Decrypting raw bytes (Size: {Size})...", ciphertext.Length);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);
            _logger.LogDebug("Raw decryption successful.");
        }
        catch (AuthenticationTagMismatchException ex)
        {
            _logger.LogWarning(ex, "Raw byte decryption failed: Authentication tag mismatch detected!");
            throw new SecurityException("Data authentication failed. The data may be tampered with or the key is incorrect.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Raw byte decryption failed due to unexpected cryptographic error.");
            throw;
        }

        return plaintextBytes;
    }
}
