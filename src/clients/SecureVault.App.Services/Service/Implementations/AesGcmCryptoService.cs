using SecureVault.App.Services.Service.Contracts;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SecureVault.App.Services.Service.Implementations
{
    public class AesGcmCryptoService : ICryptoService
    {
        private const string EncryptionKeyAlias = "EncryptionKey";
        private const int AesKeySize = 32;
        private const int NonceSize = 12;
        private const int TagSize = 16;

        public async Task<byte[]> EncryptAsync<T>(T dataToEncrypt)
        {
            var encryptionKey = await GetEncryptionKeyAsync();
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

        public async Task<T> DecryptAsync<T>(byte[] encryptedData)
        {
            var encryptionKey = await GetEncryptionKeyAsync();

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

        private async Task<byte[]> GetEncryptionKeyAsync()
        {
            var encryptionKeyHex = await SecureStorage.Default.GetAsync(EncryptionKeyAlias);
            if (string.IsNullOrEmpty(encryptionKeyHex))
            {
                throw new InvalidOperationException("Encryption key not found in SecureStorage.");
            }

            var encryptionKey = Convert.FromHexString(encryptionKeyHex);

            if (encryptionKey.Length != AesKeySize)
            {
                throw new InvalidOperationException($"Invalid key size. Key must be {AesKeySize} bytes for AES-256.");
            }

            return encryptionKey;
        }
    }
}
