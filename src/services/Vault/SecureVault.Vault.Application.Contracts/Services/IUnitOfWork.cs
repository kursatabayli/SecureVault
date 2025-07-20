namespace SecureVault.Vault.Application.Contracts.Services
{
    public interface IUnitOfWork : IDisposable
    {
        Task SaveChangesAsync();
        Task SaveChangesWithTransactionAsync();
    }
}
