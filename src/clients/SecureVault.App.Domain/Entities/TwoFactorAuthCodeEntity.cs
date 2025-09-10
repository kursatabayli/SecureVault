using SecureVault.App.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SecureVault.App.Domain.Entities
{
    public class TwoFactorAuthCodeEntity : ISynchronizableEntity
    {
        [Key]
        public Guid Id { get; private set; }
        public string Issuer { get; private set; }
        public string AccountName { get; private set; }
        public string SecretKey { get; private set; }
        public OtpType Type { get; private set; }
        public int Digits { get; private set; }
        public int Period { get; private set; }
        public long Counter { get; private set; }
        public OtpAlgorithm Algorithm { get; private set; }
        public int Version { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public bool IsSynced { get; private set; }
        public TwoFactorAuthCodeEntity() { }

        public static TwoFactorAuthCodeEntity Create(
            Guid? id,
            string issuer,
            string accountName,
            string secretKey,
            OtpType type,
            int digits,
            int period,
            long counter,
            OtpAlgorithm algorithm,
            int? version,
            DateTimeOffset? createdAt,
            DateTimeOffset? updatedAt)
        {
            var actualCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
            return new TwoFactorAuthCodeEntity
            {
                Id = id ?? Guid.NewGuid(),
                Issuer = issuer,
                AccountName = accountName,
                SecretKey = secretKey,
                Type = type,
                Digits = digits,
                Period = period,
                Counter = counter,
                Algorithm = algorithm,
                Version = version ?? 1,
                CreatedAt = actualCreatedAt,
                UpdatedAt = updatedAt ?? actualCreatedAt,
                IsDeleted = false,
                IsSynced = false
            };
        }

        public void Update(
            string issuer,
            string accountName,
            long counter,
            int version,
            DateTimeOffset updatedAt)
        {
            Issuer = issuer;
            AccountName = accountName;
            Counter = counter;
            Version = version;
            UpdatedAt = updatedAt;
            IsSynced = false;
        }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            IsSynced = false;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void MarkAsSynced()
        {
            IsSynced = true;
        }
    }
}
