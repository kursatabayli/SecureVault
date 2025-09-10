using RabbitMQ.Client;

namespace SecureVault.Shared.RabbitMQ.Contracts
{
    public interface IRabbitMqConnection : IAsyncDisposable
    {
        bool IsConnected { get; }

        Task<IChannel> CreateModelAsync(CancellationToken cancellationToken);
        Task ConnectAsync(CancellationToken cancellationToken = default);

    }
}
