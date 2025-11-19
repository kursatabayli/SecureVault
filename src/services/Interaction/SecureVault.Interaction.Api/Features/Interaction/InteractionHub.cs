using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SecureVault.Interaction.Api.Features.Interaction.Contracts;

namespace SecureVault.Interaction.Api.Features.Interaction;

[Authorize]
public class InteractionHub : Hub
{
  private readonly ILogger<InteractionHub> _logger;
  private readonly IDevicePresenceService _presenceService;

  public InteractionHub(ILogger<InteractionHub> logger, IDevicePresenceService presenceService)
  {
    _logger = logger;
    _presenceService = presenceService;
  }

  public async override Task OnConnectedAsync()
  {
    try
    {
      var userId = GetUserIdFromContext();
      if (string.IsNullOrEmpty(userId))
      {
        _logger.LogWarning("OnConnectedAsync: User ID not found in claims (sub or NameIdentifier). Connection aborted. ConnectionId: {ConnectionId}", Context.ConnectionId);
        Context.Abort();
        return;
      }
      await Groups.AddToGroupAsync(Context.ConnectionId, userId);
      _logger.LogInformation("Client connected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);
      await base.OnConnectedAsync();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error in OnConnectedAsync. ConnectionId: {ConnectionId}", Context.ConnectionId);

      Context.Abort();
    }
  }

  public async Task RegisterActiveDevice(string uniqueDeviceId)
  {
    var userId = GetUserIdFromContext();
    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(uniqueDeviceId))
    {
      _logger.LogWarning("RegisterActiveDevice failed: Missing UserId or DeviceId.");
      return;
    }

    Context.Items["UniqueDeviceId"] = uniqueDeviceId;

    await Groups.AddToGroupAsync(Context.ConnectionId, uniqueDeviceId);

    await _presenceService.RegisterDeviceAsync(userId, uniqueDeviceId);
    _logger.LogInformation("Device registered: User {UserId}, Device {DeviceId}", userId, uniqueDeviceId);

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
      await Groups.RemoveFromGroupAsync(Context.ConnectionId, uniqueDeviceId);

      await _presenceService.UnregisterDeviceAsync(userId, uniqueDeviceId);
      _logger.LogInformation("Device unregistered: User {UserId}, Device {DeviceId}", userId, uniqueDeviceId);

      await BroadcastActiveDeviceList(userId);
    }
    await base.OnDisconnectedAsync(exception);
  }

  public async Task NotifySyncRequired()
  {
    var userId = GetUserIdFromContext();
    if (string.IsNullOrEmpty(userId)) return;
    _logger.LogInformation("Sync notification received from: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);
    await Clients.OthersInGroup(userId).SendAsync("SyncRequired");
  }

  public async Task NotifyUserSessionRevoked(string userDeviceId)
  {
    _logger.LogInformation("User session revoked notification for Device: {userDeviceId}", userDeviceId);
    await Clients.Group(userDeviceId).SendAsync("UserSessionRevoked");
  }

  private async Task BroadcastActiveDeviceList(string userId)
  {
    if (string.IsNullOrEmpty(userId)) return;

    List<string> activeDeviceIds = await _presenceService.GetActiveDevicesAsync(userId);

    _logger.LogDebug("Broadcasting active device list for User {UserId}: {DeviceCount} devices.", userId, activeDeviceIds.Count);
    await Clients.Group(userId).SendAsync("ActiveDevicesUpdated", activeDeviceIds);
  }

  private string? GetUserIdFromContext()
  {
    var userIdClaim = Context.User?.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier);
    return userIdClaim?.Value;
  }
}
