namespace SecureVault.App.Application.Contracts.Abstractions.Sync;

public interface IActiveSessionTracker
{
  IReadOnlySet<string> ActiveDeviceIds { get; }
  event Func<Task>? OnChange;
}