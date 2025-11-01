using SecureVault.App.Application.Contracts.Abstractions.Sync;

namespace SecureVault.App.Infrastructure.Services.Sync;

public class ActiveSessionTracker : IActiveSessionTracker
{
  private HashSet<string> _activeDeviceIds = [];

  public IReadOnlySet<string> ActiveDeviceIds => _activeDeviceIds;

  public event Func<Task>? OnChange;

  public void UpdateActiveDevices(IEnumerable<string> deviceIds)
  {
    _activeDeviceIds = [.. deviceIds];
    NotifyStateChanged();
  }

  private void NotifyStateChanged() => OnChange?.Invoke();
}