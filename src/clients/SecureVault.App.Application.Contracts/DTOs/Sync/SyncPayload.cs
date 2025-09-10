using SecureVault.App.Application.Contracts.DTOs.VaultItem;

namespace SecureVault.App.Application.Contracts.DTOs.Sync
{
    public record SyncPayload(object DtoToEncrypt, ItemType ItemType);

}
