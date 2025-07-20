using SecureVault.Vault.Domain.Entities;
using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Application.Contracts.Repositories
{
    public interface IVaultItemsRepository
    {
        Task<VaultItem?> GetByIdAsync(Guid id);
        Task AddAsync(VaultItem vaultItem);
        IQueryable<VaultItem> GetAllUserVaultItemsByVaultTypeAsync(Guid userId, ItemType itemType);
    }
}
