using Microsoft.AspNetCore.SignalR;
using SecureVault.Interaction.Api.Features.Sync.Contracts;
using SecureVault.Interaction.Api.Features.Sync.Hubs;

namespace SecureVault.Interaction.Api.Features.Sync.Services
{
    public class SignalRNotificationService : INotificationService
    {
        private readonly IHubContext<SyncHub> _syncHubContext;
        private readonly IUserConnectionManager _userConnectionManager;

        public SignalRNotificationService(
            IHubContext<SyncHub> syncHubContext,
            IUserConnectionManager userConnectionManager)
        {
            _syncHubContext = syncHubContext;
            _userConnectionManager = userConnectionManager;
        }

        public async Task NotifyClientsForSync(Guid userId, string originDeviceId)
        {
            var connections = _userConnectionManager.GetConnections(userId, originDeviceId);

            if (connections is not null && connections.Any())
            {
                await _syncHubContext.Clients.Clients(connections).SendAsync("SyncRequired");
            }
        }
    }
}
