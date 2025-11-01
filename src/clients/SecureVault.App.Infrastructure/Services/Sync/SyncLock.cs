using SecureVault.App.Application.Contracts.Abstractions.Sync;

namespace SecureVault.App.Infrastructure.Services.Sync;

public class SyncLock : ISyncLock
{
  private readonly SemaphoreSlim _lock = new(1, 1);

  public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
  {
    return _lock.WaitAsync(timeout, cancellationToken);
  }

  public void Release()
  {
    try
    {
      _lock.Release();
    }
    catch (SemaphoreFullException)
    {
    }
  }
}
