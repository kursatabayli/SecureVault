using System.ComponentModel.DataAnnotations;

namespace SecureVault.Identity.Domain.Entities
{
    public class UserRecoveryData
    {
        [Key]
        public Guid UserId { get; private set; }
        public byte[] RecoveryData { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public virtual User User { get; private set; }

        private UserRecoveryData() { }

        public static UserRecoveryData Create(Guid userId, byte[] recoveryData)
        {
            var creationTime = DateTimeOffset.UtcNow;
            return new UserRecoveryData
            {
                UserId = userId,
                RecoveryData = recoveryData,
                CreatedAt = creationTime,
                UpdatedAt = creationTime
            };
        }

        public void UpdateEncryptedData(byte[] recoveryData)
        {
            RecoveryData = recoveryData;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
