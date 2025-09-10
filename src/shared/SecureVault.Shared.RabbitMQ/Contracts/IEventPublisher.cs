using SecureVault.Shared.Contracts.Events;

namespace SecureVault.Shared.RabbitMQ.Contracts
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T @event, string routingKey, CancellationToken cancellationToken = default)
            where T : IIntegrationEvent;
    }
}
