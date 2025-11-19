namespace SecureVault.App.Application.Contracts.Abstractions.Sync;

public interface ISyncLock
{
  Task<LockHandle> AcquireLockAsync(TimeSpan timeout, CancellationToken cancellationToken);
}

public readonly struct LockHandle : IAsyncDisposable
{
  public bool IsAcquired { get; }
  private readonly IAsyncDisposable? _releaser;

  public LockHandle(IAsyncDisposable releaser)
  {
    IsAcquired = true;
    _releaser = releaser;
  }

  public ValueTask DisposeAsync()
  {
    return _releaser?.DisposeAsync() ?? ValueTask.CompletedTask;
  }
}
