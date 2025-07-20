namespace SecureVault.App.Services.Models.VaultItemModels
{
    public class VaultItemModel
    {
        public Guid? Id { get; set; }
        public ItemType ItemType { get; set; }
        public byte[] EncryptedData { get; set; }
        public int? Version { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
