using Konscious.Security.Cryptography;
using SecureVault.Identity.Application.Contracts.Services;
using System.Security.Cryptography;
using System.Text;

namespace SecureVault.Identity.Infrastructure.Services
{
    public class HashService : IHashService
    {
        private const int SaltSize = 16;
        private const int HashLength = 64;
        private const int Iterations = 4;
        private const int MemorySize = 65536;
        private const int Parallelism = 2;

        public (string Hash, string Salt) HashItem(string Item)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(Item));
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = Parallelism;
            argon2.Iterations = Iterations;
            argon2.MemorySize = MemorySize;

            byte[] hash = argon2.GetBytes(HashLength);
            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        public bool VerifyItem(string requestItem, string hashedItem, string saltItem)
        {
            byte[] storedHash = Convert.FromBase64String(hashedItem);
            byte[] salt = Convert.FromBase64String(saltItem);

            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(requestItem));
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = Parallelism;
            argon2.Iterations = Iterations;
            argon2.MemorySize = MemorySize;

            byte[] computedHash = argon2.GetBytes(HashLength);
            return computedHash.SequenceEqual(storedHash);
        }
    }
}
