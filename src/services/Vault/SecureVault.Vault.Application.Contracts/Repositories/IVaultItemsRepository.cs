using SecureVault.Vault.Application.Contracts.Specifications;
using SecureVault.Vault.Domain.Entities;

namespace SecureVault.Vault.Application.Contracts.Repositories
{
    public interface IVaultItemsRepository
    {
        Task<VaultItem?> GetByIdAsync(Guid id);
        Task AddAsync(VaultItem vaultItem);
        Task<bool> UpdateAsync(VaultItem vaultItem);
        Task<IReadOnlyCollection<TResult>> FindAsync<TResult>(ISpecification<VaultItem, TResult> spec);
        Task<DateTimeOffset?> GetLatestUpdateTimeAsync(Guid userId);
    }
}
