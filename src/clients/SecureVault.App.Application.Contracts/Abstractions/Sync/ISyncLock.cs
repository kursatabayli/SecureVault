namespace SecureVault.App.Application.Contracts.Abstractions.Sync;

public interface ISyncLock
{
  Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);
  void Release();
}
