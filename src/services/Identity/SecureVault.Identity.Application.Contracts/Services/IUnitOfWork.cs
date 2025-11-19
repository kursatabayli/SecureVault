namespace SecureVault.Identity.Application.Contracts.Services;

public interface IUnitOfWork : IAsyncDisposable
{
    Task SaveChangesAsync();
    Task SaveChangesWithTransactionAsync();
}
