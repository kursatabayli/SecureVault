namespace SecureVault.App.Application.Contracts.Abstractions.Sync
{
    public interface ISyncConnectionService
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task DisconnectAsync();
        Task NotifySyncRequiredAsync(CancellationToken cancellationToken = default);
    }
}
