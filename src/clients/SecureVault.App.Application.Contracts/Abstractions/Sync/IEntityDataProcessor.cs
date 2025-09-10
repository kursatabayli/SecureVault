using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.App.Application.Contracts.Repositories;

namespace SecureVault.App.Application.Contracts.Abstractions.Sync
{
    public interface IEntityDataProcessor
    {
        ItemType Type { get; }
        Task ProcessItemAsync(IUnitOfWork unitOfWork, VaultItemDto serverItem);
    }
}
