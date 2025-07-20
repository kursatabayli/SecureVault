using Konscious.Security.Cryptography;
using PasswordGenerator;
using SecureVault.App.Services.Service.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace SecureVault.App.Services.Service.Implementations
{
    public class HashService : IHashService
    {
        public byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
        public byte[] CreateMasterSecret(string password, byte[] salt)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = salt,
                DegreeOfParallelism = 2,
                Iterations = 4,
                MemorySize = 65536
            };

            return argon2.GetBytes(32);
        }
        public byte[] GetPrivateKeyForAuth(byte[] masterSecret, byte[] salt)
        {
            if (masterSecret == null) throw new InvalidOperationException("User is not logged in.");

            return HKDF.DeriveKey(
                hashAlgorithmName: HashAlgorithmName.SHA256,
                ikm: masterSecret,
                outputLength: 32,
                salt: salt,
                info: Encoding.UTF8.GetBytes("schnorr-auth-key-v1")
            );
        }

        public byte[] GetEncryptionKeyForData(byte[] masterSecret, byte[] salt)
        {
            if (masterSecret == null) throw new InvalidOperationException("User is not logged in.");

            return HKDF.DeriveKey(
                hashAlgorithmName: HashAlgorithmName.SHA256,
                ikm: masterSecret,
                outputLength: 32,
                salt: salt,
                info: Encoding.UTF8.GetBytes("aes-gcm-data-key-v1")
            );
        }

        public string GeneratePassword()
        {
            var pwdGen = new Password();
            return pwdGen.Next();
        }
    }
}
