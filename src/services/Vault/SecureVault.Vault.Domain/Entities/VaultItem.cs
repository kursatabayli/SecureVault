using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Domain.Entities
{
    public class VaultItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; private set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.String)]
        public Guid UserId { get; private set; }

        [BsonElement("itemType")]
        [BsonRepresentation(BsonType.String)]
        public ItemType ItemType { get; private set; }

        [BsonElement("encryptedData")]
        public byte[] EncryptedData { get; private set; }

        [BsonElement("version")]
        public int Version { get; private set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; private set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; private set; }

        [BsonElement("isDeleted")]
        public bool IsDeleted { get; private set; }

        private VaultItem() { }

        public static VaultItem Create(Guid userId, ItemType itemType, byte[] encryptedData)
        {
            var creationTime = DateTime.UtcNow;
            return new VaultItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ItemType = itemType,
                EncryptedData = encryptedData,
                Version = 1,
                CreatedAt = creationTime,
                UpdatedAt = creationTime
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
