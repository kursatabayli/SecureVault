using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace SecureVault.Interaction.Api.Features.Sync.Hubs
{
    [Authorize]
    public class SyncHub : Hub
    {
        private readonly ILogger<SyncHub> _logger;
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _activeDevicesByUser = new();
        public SyncHub(ILogger<SyncHub> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId))
            {
                Context.Abort();
                return Task.CompletedTask;
            }
            Groups.AddToGroupAsync(Context.ConnectionId, userId);
            _logger.LogInformation("Client connected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);
            return base.OnConnectedAsync();
        }
        public async Task NotifySyncRequired()
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            _logger.LogInformation("Sync notification received from: {ConnectionId}, User: {UserId}. Notifying others in group.", Context.ConnectionId, userId);

            await Clients.OthersInGroup(userId).SendAsync("SyncRequired");
        }

        public async Task RegisterActiveDevice(string uniqueDeviceId)
        {
            var userId = GetUserIdFromContext();
            var connectionId = Context.ConnectionId;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(uniqueDeviceId))
            {
                _logger.LogWarning("RegisterActiveDevice failed: Missing UserId or DeviceId. User: {UserId}, Device: {DeviceId}", userId, uniqueDeviceId);
                return;
            }

            Context.Items["UniqueDeviceId"] = uniqueDeviceId;

            var userDevices = _activeDevicesByUser.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
            if (userDevices.TryAdd(uniqueDeviceId, 0))
            {
                _logger.LogInformation("Device registered: User {UserId}, Device {DeviceId}, Connection {ConnectionId}", userId, uniqueDeviceId, connectionId);
            }

            await BroadcastActiveDeviceList(userId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId))
            {
                await base.OnDisconnectedAsync(exception);
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            _logger.LogInformation("Client disconnected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);

            if (Context.Items.TryGetValue("UniqueDeviceId", out var deviceIdObj) && deviceIdObj is string uniqueDeviceId)
            {
                if (_activeDevicesByUser.TryGetValue(userId, out var userDevices))
                {
                    if (userDevices.TryRemove(uniqueDeviceId, out _))
                    {
                        _logger.LogInformation("Device unregistered: User {UserId}, Device {DeviceId}", userId, uniqueDeviceId);
                    }

                    if (userDevices.IsEmpty)
                    {
                        _activeDevicesByUser.TryRemove(userId, out _);
                    }
                }
            }

            await BroadcastActiveDeviceList(userId);

            await base.OnDisconnectedAsync(exception);
        }

        private async Task BroadcastActiveDeviceList(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            List<string> activeDeviceIds = new List<string>();

            if (_activeDevicesByUser.TryGetValue(userId, out var userDevices))
            {
                activeDeviceIds = userDevices.Keys.ToList();
            }

            _logger.LogDebug("Broadcasting active device list for User {UserId}: {DeviceCount} devices.", userId, activeDeviceIds.Count);
            await Clients.Group(userId).SendAsync("ActiveDevicesUpdated", activeDeviceIds);
        }

        private string? GetUserIdFromContext()
        {
            var userIdClaim = Context.User?.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier);
            return userIdClaim?.Value;
        }
    }
}
