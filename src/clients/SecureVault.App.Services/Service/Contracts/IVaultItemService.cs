using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services.Service.Contracts
{
    public interface IVaultItemService<T> where T : class, IVaultItemData, new()
    {
        Task<Result<IReadOnlyCollection<T>>> GetVaultItemsByItemTypeAsync(ItemType itemType);
        Task<Result> CreateVaultItem(T Data, ItemType itemType);
        Task<Result> UpdateVaultItem(T Data);
    }
}
