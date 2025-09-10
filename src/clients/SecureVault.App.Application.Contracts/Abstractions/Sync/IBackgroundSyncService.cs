namespace SecureVault.App.Application.Contracts.Abstractions.Sync
{
    public interface IBackgroundSyncService
    {
        Task SynchronizeAsync(CancellationToken cancellationToken);
    }
}
