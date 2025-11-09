namespace SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin
{
    public interface IQrHubConnection : IAsyncDisposable
    {
        event Func<string, string, Task> OnMessageReceived;
        event Func<string, Task> OnErrorReceived;
        Task<string> ConnectAndCreateChannelAsync(CancellationToken cancellationToken = default);
        Task ConnectAndJoinChannelAsync(string channelId, CancellationToken cancellationToken = default);
        Task<string> SwitchChannelAsync(CancellationToken cancellationToken = default);
        Task SendMessageAsync(string messageType, string payload, CancellationToken cancellationToken = default);
    }
}
