using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Interaction;
using SecureVault.App.Application.Contracts.Abstractions.Sync;

namespace SecureVault.App.Infrastructure.Services.Interaction.Handlers;

public class SyncRequiredHandler : ISignalRHubEventHandler
{
  private readonly ILogger<SyncRequiredHandler> _logger;
  private readonly IBackgroundSyncService _backgroundSyncService;

  public SyncRequiredHandler(ILogger<SyncRequiredHandler> logger, IBackgroundSyncService backgroundSyncService)
  {
    _logger = logger;
    _backgroundSyncService = backgroundSyncService;
  }

  public void RegisterHandlers(HubConnection connection)
  {
    connection.On("SyncRequired", async () =>
    {
      try
      {
        _logger.LogInformation("Received 'SyncRequired' notification from server. Triggering Catch-Up Sync.");
        await _backgroundSyncService.SynchronizeAsync(CancellationToken.None);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error handling 'SyncRequired' notification.");
      }
    });
  }
}
