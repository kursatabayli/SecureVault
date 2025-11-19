using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Results;

public class GetAllUserVaultItemsByLastSyncTimeResult
{
    public Guid Id { get; set; }
    public ItemType ItemType { get; set; }
    public byte[] EncryptedData { get; set; }
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

}
