using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Contracts.Abstractions.Api
{
    public interface IVaultItemService
    {
        Task<Result<IReadOnlyCollection<VaultItemDto>>> GetUserVaultAsync(CancellationToken cancellationToken);
        Task<Result<IReadOnlyCollection<VaultItemDto>>> GetVaultItemsByItemTypeAsync(ItemType itemType, CancellationToken cancellationToken);
        Task<Result> CreateVaultItemAsync(CreateVaultItemDto createVaultItemDto, CancellationToken cancellationToken);
        Task<Result> UpdateVaultItemAsync(UpdateVaultItemDto UpdateEncryptedData, CancellationToken cancellationToken);
        Task<Result> DeleteVaultItemAsync(Guid id, CancellationToken cancellationToken);
        Task<Result> CreateVaultItemsAsync(IList<CreateVaultItemDto> createVaultItemDto, CancellationToken cancellationToken);
        Task<Result> UpdateVaultItemsAsync(IList<UpdateVaultItemDto> UpdateEncryptedData, CancellationToken cancellationToken);
        Task<Result> DeleteVaultItemsAsync(IList<Guid> ids, CancellationToken cancellationToken);
        Task<Result<IReadOnlyCollection<VaultItemDto>>> GetVaultItemByLastSyncTimeAsync(DateTimeOffset lastUpdateTime, CancellationToken cancellationToken);
    }
}
