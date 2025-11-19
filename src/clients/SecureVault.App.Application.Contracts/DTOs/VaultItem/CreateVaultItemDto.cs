namespace SecureVault.App.Application.Contracts.DTOs.VaultItem;

public class CreateVaultItemDto
{
    public Guid Id { get; set; }
    public ItemType ItemType { get; set; }
    public byte[] EncryptedData { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
