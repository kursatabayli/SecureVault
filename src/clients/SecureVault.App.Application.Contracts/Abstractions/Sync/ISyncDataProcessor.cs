using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Contracts.Abstractions.Sync
{
    public interface ISyncDataProcessor
    {
        Task<Result> ProcessServerDataAsync(IReadOnlyCollection<VaultItemDto> serverItems, IUnitOfWork unitOfWork);
    }
}
