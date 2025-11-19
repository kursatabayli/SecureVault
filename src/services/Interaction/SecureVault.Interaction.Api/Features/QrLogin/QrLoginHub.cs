using Microsoft.AspNetCore.SignalR;
using SecureVault.Interaction.Api.Features.QrLogin.Contracts;
using Serilog;

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

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext == null)
        {
            _logger.LogError("OnConnectedAsync: HttpContext is null. ConnectionId: {ConnectionId}", Context.ConnectionId);
            Context.Abort();
            return;
        }

        string channelId = httpContext.Request.Headers["X-Channel-Id"];
        if (string.IsNullOrEmpty(channelId))
        {
            _logger.LogWarning("OnConnectedAsync: X-Channel-Id header is missing or empty. Connection aborted. ConnectionId: {ConnectionId}", Context.ConnectionId);
            Context.Abort();
            return;
        }

        _logger.LogInformation("OnConnectedAsync: Client connecting. ChannelId: {ChannelId}, ConnectionId: {ConnectionId}", channelId, Context.ConnectionId);

        try
        {
            var (success, newCount, role) = await _channelService.RegisterConnectionAsync(channelId, Context.ConnectionId);

            if (!success)
            {
                _logger.LogWarning("OnConnectedAsync: Channel registration failed. ChannelId: {ChannelId}, ConnId: {ConnectionId}, Reported Count: {Count}", channelId, Context.ConnectionId, newCount);
                await Clients.Caller.SendAsync("Error", "ChannelIsFullOrBusy");
                Context.Abort();
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, channelId);
            _logger.LogInformation("OnConnectedAsync: Client added to group. Role: {Role}, ChannelId: {ChannelId}, ConnId: {ConnectionId}, Total Members: {UserCount}", role, channelId, Context.ConnectionId, newCount);
            if (role == "Joiner" && newCount == 2)
            {
                _logger.LogInformation("OnConnectedAsync: Channel is ready. Sending 'ChannelReady' to group. ChannelId: {ChannelId}", channelId);
                await Clients.Group(channelId).SendAsync("ReceiveMessage", "ChannelReady", string.Empty);
            }

            await Clients.Caller.SendAsync("Connected", role);

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in OnConnectedAsync. ChannelId: {ChannelId}, ConnectionId: {ConnectionId}", channelId, Context.ConnectionId);
            Context.Abort();
        }
    }

    public async Task SwitchToNewChannel(string newChannelId)
    {
        _logger.LogInformation("Client {ConnectionId} requested to switch channel.", Context.ConnectionId);

        var oldChannelId = await _channelService.LeaveChannelAsync(Context.ConnectionId);
        if (oldChannelId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, oldChannelId);
            _logger.LogInformation("Client {ConnectionId} removed from old channel {OldChannelId}.", Context.ConnectionId, oldChannelId);
        }
        else
        {
            _logger.LogWarning("Client {ConnectionId} requested switch, but was not in any channel.", Context.ConnectionId);
        }
        var (success, newCount, role) = await _channelService.RegisterConnectionAsync(newChannelId, Context.ConnectionId);

        if (!success)
        {
            _logger.LogError("SwitchToNewChannel: Registration for new channel FAILED. {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "FailedToSwitchChannel");
            throw new InvalidOperationException("Failed to register new channel during switch.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, newChannelId);

        _logger.LogInformation("Client {ConnectionId} switched to new channel {NewChannelId}. Role: {Role}, Count: {NewCount}", Context.ConnectionId, newChannelId, role, newCount);

    }

    public async Task SendMessageToChannel(string channelId, string messageType, string? payload)
    {
        _logger.LogInformation("Sending message. Type: {MessageType}, ChannelId: {ChannelId}, FromConnection: {ConnectionId}", messageType, channelId, Context.ConnectionId);
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
