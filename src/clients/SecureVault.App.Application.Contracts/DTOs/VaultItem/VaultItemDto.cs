namespace SecureVault.App.Application.Contracts.DTOs.VaultItem
{
    public class VaultItemDto
    {
        public Guid Id { get; set; }
        public ItemType ItemType { get; set; }
        public byte[] EncryptedData { get; set; }
        public int Version { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
