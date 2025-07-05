using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Domain.Entities
{
    public class VaultItem
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public ItemType ItemType { get; private set; }
        public byte[] EncryptedData { get; private set; }
        public int Version { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public bool IsDeleted { get; private set; }

        private VaultItem() { }

        public static VaultItem Create(Guid userId, ItemType itemType, byte[] encryptedData)
        {
            return new VaultItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ItemType = itemType,
                EncryptedData = encryptedData,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        public void UpdateData(byte[] newEncryptedData)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot update a deleted item.");

            EncryptedData = newEncryptedData ?? [];
            Version++;
            UpdatedAt = DateTime.UtcNow;
        }
        public void Delete()
        {
            if (!IsDeleted)
            {
                IsDeleted = true;
                UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
