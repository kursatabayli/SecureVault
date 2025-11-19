namespace SecureVault.App.Application.Contracts.Abstractions.Sync;

public interface IBackgroundSyncService
{
    Task<bool> SynchronizeAsync(CancellationToken cancellationToken);
}
