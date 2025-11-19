using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Sync;

namespace SecureVault.App.Infrastructure.Services.Sync;

public class ActiveSessionTracker : IActiveSessionTracker
{
  private HashSet<string> _activeDeviceIds = [];
  private readonly object _lock = new();
  private readonly ILogger<ActiveSessionTracker> _logger;

  public event Func<Task>? OnChange;

  public ActiveSessionTracker(ILogger<ActiveSessionTracker> logger)
  {
    _logger = logger;
  }

  public IReadOnlySet<string> ActiveDeviceIds
  {
    get
    {
      lock (_lock)
      {
        return _activeDeviceIds;
      }
    }
  }

  public void UpdateActiveDevices(IEnumerable<string> deviceIds)
  {
    var newDeviceSet = deviceIds.ToHashSet();
    HashSet<string> oldDeviceSet;

    lock (_lock)
    {
      oldDeviceSet = _activeDeviceIds;
      _activeDeviceIds = newDeviceSet;
    }

    _logger.LogInformation(
        "Active device list updated. Old count: {OldCount}, New count: {NewCount}",
        oldDeviceSet.Count,
        newDeviceSet.Count);

    if (_logger.IsEnabled(LogLevel.Debug))
    {
      _logger.LogDebug("New device list: {DeviceIds}", string.Join(", ", newDeviceSet));
    }

    NotifyStateChangedSafely();
  }

  private void NotifyStateChangedSafely()
  {
    if (OnChange == null)
      return;

    var invocationList = OnChange.GetInvocationList();
    _logger.LogTrace("Notifying {HandlerCount} ActiveSessionTracker subscribers...", invocationList.Length);

    _ = Task.Run(async () =>
    {
      foreach (Func<Task> handler in invocationList)
      {
        try
        {
          await handler();
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error in ActiveSessionTracker.OnChange subscriber.");
        }
      }
    });
  }
}