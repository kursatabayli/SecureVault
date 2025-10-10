using SecureVault.Interaction.Api.Features.Sync.Contracts;
using SecureVault.Shared.Contracts.Events;

namespace SecureVault.Interaction.Api.Features.Sync.EventHandlers
{
    public class UserActivityOccurredIntegrationEventHandler : IIntegrationEventHandler<UserActivityOccurredIntegrationEvent>
    {
        private readonly ILogger<UserActivityOccurredIntegrationEventHandler> _logger;
        private readonly INotificationService _notificationService;

        public UserActivityOccurredIntegrationEventHandler(
            ILogger<UserActivityOccurredIntegrationEventHandler> logger,
            INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task Handle(UserActivityOccurredIntegrationEvent @event)
        {
            _logger.LogInformation("UserId: {UserId} için senkronizasyon bildirimi gönderiliyor. Kaynak Cihaz: {DeviceId}", @event.UserId, @event.OriginDeviceId);
            await _notificationService.NotifyClientsForSync(@event.UserId, @event.OriginDeviceId);
        }
    }
}
