using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Results
{
    public class VaultItemResult
    {
        public Guid Id { get; set; }
        public ItemType ItemType { get; set; }
        public byte[] EncryptedData { get; set; }
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
