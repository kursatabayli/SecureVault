using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Sync;

namespace SecureVault.App.Infrastructure.Services.Sync;

public class SyncLock : ISyncLock, IAsyncDisposable
{
  private readonly SemaphoreSlim _lock = new(1, 1);
  private readonly ILogger<SyncLock> _logger;

  public SyncLock(ILogger<SyncLock> logger)
  {
    _logger = logger;
  }

  public async Task<LockHandle> AcquireLockAsync(TimeSpan timeout, CancellationToken cancellationToken)
  {
    _logger.LogTrace("Attempting to acquire sync lock...");
    var acquired = await _lock.WaitAsync(timeout, cancellationToken);

    if (acquired)
    {
      _logger.LogDebug("Sync lock acquired.");
      return new LockHandle(this);
    }

    _logger.LogWarning("Failed to acquire sync lock within timeout ({Timeout}). Sync process will be skipped.", timeout);
    return default;
  }

  public ValueTask DisposeAsync()
  {
    try
    {
      _lock.Release();
      _logger.LogDebug("Sync lock released (via DisposeAsync).");
    }
    catch (SemaphoreFullException ex)
    {
      _logger.LogWarning(ex, "Sync lock 'Release' (via DisposeAsync) called but semaphore was already at its maximum count.");
    }
    return ValueTask.CompletedTask;
  }
}
