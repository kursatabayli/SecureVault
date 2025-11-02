using Microsoft.AspNetCore.SignalR;
using SecureVault.Interaction.Api.Features.QrLogin.Contracts;

namespace SecureVault.Interaction.Api.Features.QrLogin;

public class QrLoginHub : Hub
{
    private readonly IQrLoginChannelService _channelService;
    private readonly ILogger<QrLoginHub> _logger;

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

        var connectionCount = await _channelService.JoinChannelAsync(channelId, Context.ConnectionId);

        _logger.LogInformation("Client joined channel. ConnectionId: {ConnectionId}, ChannelId: {ChannelId}, Total Users: {UserCount}", Context.ConnectionId, channelId, connectionCount);

        if (connectionCount > 2)
        {
            _logger.LogWarning("Channel is full. Kicking user. ChannelId: {ChannelId}, ConnectionId: {ConnectionId}", channelId, Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "ChannelIsFull");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);

            await _channelService.LeaveChannelAsync(Context.ConnectionId);
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
            _logger.LogError(exception, "Client disconnected with error. ConnectionId: {ConnectionId}", Context.ConnectionId);

        var channelId = await _channelService.LeaveChannelAsync(Context.ConnectionId);

        if (channelId != null)
            _logger.LogInformation("Client disconnected. ConnectionId: {ConnectionId}, ChannelId: {ChannelId}", Context.ConnectionId, channelId);
        else
            _logger.LogInformation("A client disconnected without joining a channel. ConnectionId: {ConnectionId}", Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }
}
