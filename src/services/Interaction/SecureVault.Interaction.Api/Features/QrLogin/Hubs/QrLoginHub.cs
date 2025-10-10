using Microsoft.AspNetCore.SignalR;
using SecureVault.Interaction.Api.Features.QrLogin.Contracts;
using System.Collections.Concurrent;

namespace SecureVault.Interaction.Api.Features.QrLogin.Hubs
{
    public class QrLoginHub : Hub
    {
        private readonly IQrLoginChannelService _channelService;
        private readonly ILogger<QrLoginHub> _logger;
        private static readonly ConcurrentDictionary<string, int> _groupConnectionCounts = new();
        private static readonly ConcurrentDictionary<string, string> _connectionIdToChannelIdMap = new();
        public QrLoginHub(IQrLoginChannelService channelService, ILogger<QrLoginHub> logger)
        {
            _channelService = channelService;
            _logger = logger;
        }

        public async Task JoinChannel(string channelId)
        {
            if (!await _channelService.ValidateChannelAsync(channelId))
            {
                await Clients.Caller.SendAsync("Error", "InvalidOrExpiredChannel");
                _logger.LogWarning("Invalid join attempt. ChannelId: {ChannelId}, ConnectionId: {ConnectionId}", channelId, Context.ConnectionId);
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, channelId);
            _connectionIdToChannelIdMap[Context.ConnectionId] = channelId;
            var connectionCount = _groupConnectionCounts.AddOrUpdate(channelId, 1, (key, count) => count + 1);
            _logger.LogInformation("Client joined channel. ConnectionId: {ConnectionId}, ChannelId: {ChannelId}, Total Users: {UserCount}", Context.ConnectionId, channelId, connectionCount);

            if (connectionCount > 2)
            {
                _logger.LogWarning("Channel is full. Kicking user. ChannelId: {ChannelId}, ConnectionId: {ConnectionId}", channelId, Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "ChannelIsFull");
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
                _groupConnectionCounts.TryUpdate(channelId, connectionCount - 1, connectionCount);
            }
            if (connectionCount == 2)
            {
                _logger.LogInformation("Channel is ready. ChannelId: {ChannelId}", channelId);
                await Clients.Group(channelId).SendAsync("ReceiveMessage", "ChannelReady", string.Empty);
            }
        }

        public async Task SendMessageToChannel(string channelId, string messageType, string? payload)
        {
            await Clients.OthersInGroup(channelId).SendAsync("ReceiveMessage", messageType, payload);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogError(exception, "Client disconnected with error. ConnectionId: {ConnectionId}", Context.ConnectionId);
            }

            if (_connectionIdToChannelIdMap.TryRemove(Context.ConnectionId, out var channelId))
            {
                _logger.LogInformation("Client disconnected. ConnectionId: {ConnectionId}, ChannelId: {ChannelId}", Context.ConnectionId, channelId);

                if (_groupConnectionCounts.TryGetValue(channelId, out var currentCount))
                {
                    _groupConnectionCounts.TryUpdate(channelId, currentCount - 1, currentCount);
                }
            }
            else
            {
                _logger.LogInformation("A client disconnected without joining a channel. ConnectionId: {ConnectionId}", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
