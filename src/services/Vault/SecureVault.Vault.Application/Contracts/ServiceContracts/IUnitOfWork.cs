using SecureVault.Vault.Application.Contracts.RepositoryContracts;

namespace SecureVault.Vault.Application.Contracts.ServicesContracts
{
    public interface IUnitOfWork : IDisposable
    {
        Task SaveChangesAsync();
        Task SaveChangesWithTransactionAsync();
    }
}
