using SecureVault.App.Services.Service.Contracts;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SecureVault.App.Services.Service.Implementations
{
    public class AesGcmCryptoService : ICryptoService
    {
        private const int AesKeySize = 32;
        private const int NonceSize = 12;
        private const int TagSize = 16;

        public byte[] Encrypt<T>(T dataToEncrypt, byte[] encryptionKey)
        {
            var plaintextBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dataToEncrypt));

            using var aesGcm = new AesGcm(encryptionKey);

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
        public T Decrypt<T>(byte[] encryptedData, byte[] encryptionKey)
        {
            if (encryptedData.Length < NonceSize + TagSize)
                throw new CryptographicException("Invalid encrypted data format.");

            var nonce = new ReadOnlySpan<byte>(encryptedData, 0, NonceSize);
            var tag = new ReadOnlySpan<byte>(encryptedData, NonceSize, TagSize);
            var ciphertext = new ReadOnlySpan<byte>(encryptedData, NonceSize + TagSize, encryptedData.Length - (NonceSize + TagSize));

            using var aesGcm = new AesGcm(encryptionKey);

            var plaintextBytes = new byte[ciphertext.Length];

            try
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);
            }
            catch (AuthenticationTagMismatchException ex)
            {
                throw new SecurityException("Data authentication failed. The data may be tampered with or the key is incorrect.", ex);
            }

            var jsonString = Encoding.UTF8.GetString(plaintextBytes);
            return JsonSerializer.Deserialize<T>(jsonString);
        }


        public byte[] EncryptBytes(byte[] plaintextBytes, byte[] encryptionKey)
        {
            if (encryptionKey.Length != AesKeySize)
                throw new ArgumentException($"Invalid key size. Key must be {AesKeySize} bytes.", nameof(encryptionKey));

            using var aesGcm = new AesGcm(encryptionKey);
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
                throw new ArgumentException($"Invalid key size. Key must be {AesKeySize} bytes.", nameof(encryptionKey));

            if (encryptedData.Length < NonceSize + TagSize)
                throw new CryptographicException("Invalid encrypted data format.");

            var nonce = new ReadOnlySpan<byte>(encryptedData, 0, NonceSize);
            var tag = new ReadOnlySpan<byte>(encryptedData, NonceSize, TagSize);
            var ciphertext = new ReadOnlySpan<byte>(encryptedData, NonceSize + TagSize, encryptedData.Length - (NonceSize + TagSize));

            using var aesGcm = new AesGcm(encryptionKey);
            var plaintextBytes = new byte[ciphertext.Length];

            try
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);
            }
            catch (AuthenticationTagMismatchException ex)
            {
                throw new SecurityException("Data authentication failed. The data may be tampered with or the key is incorrect.", ex);
            }

            return plaintextBytes;
        }
    }
}
