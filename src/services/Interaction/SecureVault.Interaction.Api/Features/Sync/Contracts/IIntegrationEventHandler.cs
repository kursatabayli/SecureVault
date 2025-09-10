using SecureVault.Shared.Contracts.Events;

namespace SecureVault.Interaction.Api.Features.Sync.Contracts
{
    public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
    {
        Task Handle(TEvent @event);
    }
}
