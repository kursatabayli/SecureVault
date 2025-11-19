namespace SecureVault.App.Application.Contracts.Abstractions.Interaction;

public interface IInteractionConnectionService : IDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task DisconnectAsync();
    Task NotifySyncRequiredAsync(CancellationToken cancellationToken = default);
    Task NotifyUserSessionRevokedAsync(string userDeviceId, CancellationToken cancellationToken = default);
}
