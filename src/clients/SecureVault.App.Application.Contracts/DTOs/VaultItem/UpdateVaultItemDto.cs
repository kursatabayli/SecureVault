namespace SecureVault.App.Application.Contracts.DTOs.VaultItem
{
    public class UpdateVaultItemDto
    {
        public Guid Id { get; set; }
        public byte[] EncryptedData { get; set; }
        public int Version { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
