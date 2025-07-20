namespace SecureVault.App.Services.Models.VaultItemModels
{
    public interface IVaultItemData
    {
        Guid Id { get; set; }
        DateTime CreatedAt { get; set; }
    }
}
