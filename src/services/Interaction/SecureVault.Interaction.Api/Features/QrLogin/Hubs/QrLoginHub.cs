using Microsoft.AspNetCore.SignalR;
using SecureVault.Interaction.Api.Features.QrLogin.Contracts;
using System.Collections.Concurrent;

namespace SecureVault.Interaction.Api.Features.QrLogin.Hubs
{
    public class QrLoginHub : Hub
    {
        private readonly IQrLoginChannelService _channelService;
        private static readonly ConcurrentDictionary<string, int> _groupConnectionCounts = new();
        private static readonly ConcurrentDictionary<string, string> _connectionIdToChannelIdMap = new();
        public QrLoginHub(IQrLoginChannelService channelService)
        {
            _channelService = channelService;
        }

        public async Task JoinChannel(string channelId)
        {
            if (!await _channelService.ValidateChannelAsync(channelId))
            {
                await Clients.Caller.SendAsync("Error", "InvalidOrExpiredChannel");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, channelId);
            _connectionIdToChannelIdMap[Context.ConnectionId] = channelId;
            var connectionCount = _groupConnectionCounts.AddOrUpdate(channelId, 1, (key, count) => count + 1);

            if (connectionCount == 2)
            {
                await Clients.Group(channelId).SendAsync("StartKeyExchange");
            }
            else if (connectionCount > 2)
            {
                await Clients.Caller.SendAsync("Error", "ChannelIsFull");
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
                _groupConnectionCounts.TryUpdate(channelId, connectionCount - 1, connectionCount);
            }
        }

        public async Task SendMessageToChannel(string channelId, string messageType, string payload)
        {
            await Clients.OthersInGroup(channelId).SendAsync("ReceiveMessage", messageType, payload);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectionIdToChannelIdMap.TryRemove(Context.ConnectionId, out var channelId))
            {
                if (_groupConnectionCounts.TryGetValue(channelId, out var currentCount))
                {
                    _groupConnectionCounts.TryUpdate(channelId, currentCount - 1, currentCount);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
