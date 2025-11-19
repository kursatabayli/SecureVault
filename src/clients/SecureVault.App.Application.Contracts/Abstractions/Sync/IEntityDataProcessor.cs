using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Contracts.Abstractions.Sync;

public interface IEntityDataProcessor
{
    ItemType Type { get; }
    Task<Result> ProcessServerDataAsync(IReadOnlyCollection<VaultItemDto> allServerItems, byte[] encryptionKey);
}
