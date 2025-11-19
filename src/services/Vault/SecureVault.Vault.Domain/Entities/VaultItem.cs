using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Domain.Entities;

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
    public DateTimeOffset CreatedAt { get; private set; }

    [BsonElement("updatedAt")]
    public DateTimeOffset UpdatedAt { get; private set; }

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; private set; }

    [BsonElement("lastUpdatedByDeviceId")]
    [BsonRepresentation(BsonType.String)]
    public string LastUpdatedByDeviceId { get; private set; }
    private VaultItem() { }

    public static VaultItem Create(Guid id, Guid userId, ItemType itemType, byte[] encryptedData, DateTimeOffset creationTime, string deviceId)
    {
        return new VaultItem
        {
            Id = id,
            UserId = userId,
            ItemType = itemType,
            EncryptedData = encryptedData,
            Version = 1,
            CreatedAt = creationTime,
            UpdatedAt = creationTime,
            LastUpdatedByDeviceId = deviceId,
        };
    }
    public void UpdateData(byte[] newEncryptedData, DateTimeOffset updatedAt, string deviceId)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted item.");

        EncryptedData = newEncryptedData ?? [];
        Version++;
        UpdatedAt = updatedAt;
        LastUpdatedByDeviceId = deviceId;
    }
    public void Delete(string deviceId)
    {
        if (!IsDeleted)
        {
            IsDeleted = true;
            UpdatedAt = DateTimeOffset.UtcNow;
            LastUpdatedByDeviceId = deviceId;
        }
    }
}
