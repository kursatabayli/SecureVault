namespace SecureVault.App.Application.Contracts.Abstractions.Interaction
{
    public interface IInteractionConnectionService
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task DisconnectAsync();
        Task NotifySyncRequiredAsync(CancellationToken cancellationToken = default);
        Task NotifyUserSessionRevokedAsync(string userDeviceId, CancellationToken cancellationToken = default);
    }
}
