using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Interaction;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Infrastructure.Services.Sync;

namespace SecureVault.App.Infrastructure.Services.Interaction.Handlers;

public class ActiveDevicesUpdatedHandler : ISignalRHubEventHandler
{
  private readonly ILogger<ActiveDevicesUpdatedHandler> _logger;
  private readonly IActiveSessionTracker _activeSessionTracker;

  public ActiveDevicesUpdatedHandler(ILogger<ActiveDevicesUpdatedHandler> logger, IActiveSessionTracker activeSessionTracker)
  {
    _logger = logger;
    _activeSessionTracker = activeSessionTracker;
  }

  public void RegisterHandlers(HubConnection connection)
  {
    connection.On<List<string>>("ActiveDevicesUpdated", (deviceIds) =>
    {
      try
      {
        _logger.LogInformation("Active device list updated. {Count} devices are active.", deviceIds.Count);
        (_activeSessionTracker as ActiveSessionTracker)?.UpdateActiveDevices(deviceIds);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error handling 'ActiveDevicesUpdated' notification.");
      }
    });
  }
}
