using SecureVault.Vault.Application.Contracts.Specifications;
using SecureVault.Vault.Domain.Entities;

namespace SecureVault.Vault.Application.Contracts.Repositories
{
    public interface IVaultItemsRepository
    {
        Task<VaultItem?> GetByIdAsync(Guid id);
        Task<IEnumerable<VaultItem>> GetByIdsAsync(IEnumerable<Guid> ids);
        Task AddAsync(VaultItem vaultItem);
        Task AddRangeAsync(IEnumerable<VaultItem> vaultItems);
        Task<bool> UpdateAsync(VaultItem vaultItem);
        Task<bool> UpdateRangeAsync(IEnumerable<VaultItem> vaultItems);
        Task<IReadOnlyCollection<TResult>> FindAsync<TResult>(ISpecification<VaultItem, TResult> spec);
        Task<DateTimeOffset?> GetLatestUpdateTimeAsync(Guid userId);
    }
}
