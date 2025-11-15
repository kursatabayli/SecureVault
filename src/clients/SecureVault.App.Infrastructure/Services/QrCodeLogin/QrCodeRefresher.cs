using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin;

public class QrCodeRefresher : IQrCodeRefresher
{
  private readonly ILogger<QrCodeRefresher> _logger;

  public event Func<string, Task> OnQrCodeAvailable;
  public event Func<string, Task> OnError;

  private CancellationTokenSource _refreshCts;
  private Task _refreshTask;

  public QrCodeRefresher(ILogger<QrCodeRefresher> logger)
  {
    _logger = logger;
  }

  public Task StartAsync(Func<Task<string>> switchChannelCallback, TimeSpan interval, CancellationToken cancellationToken)
  {
    if (_refreshTask != null && !_refreshTask.IsCompleted)
    {
      _logger.LogWarning("Refresh timer is already running. Stopping the previous one before starting anew.");
      _refreshCts?.Cancel();
    }

    _refreshCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    _refreshTask = RunRefreshLoopAsync(switchChannelCallback, interval, _refreshCts.Token);
    return Task.CompletedTask;
  }

  public async Task StopAsync()
  {
    if (_refreshCts != null && !_refreshCts.IsCancellationRequested)
    {
      _logger.LogInformation("Stopping QR refresh timer...");
      _refreshCts.Cancel();
    }

    if (_refreshTask != null)
    {
      try
      {
        await _refreshTask;
      }
      catch (OperationCanceledException)
      {
        _logger.LogInformation("QR refresh task cancelled successfully.");
      }
      finally
      {
        _refreshTask = null;
      }
    }
  }

  private async Task RunRefreshLoopAsync(Func<Task<string>> switchChannelCallback, TimeSpan interval, CancellationToken cancellationToken)
  {
    using var timer = new PeriodicTimer(interval);

    try
    {
      while (await timer.WaitForNextTickAsync(cancellationToken))
      {
        try
        {
          _logger.LogInformation("QR refresh timer ticked. Executing switch channel callback...");

          var newChannelId = await switchChannelCallback();

          if (!string.IsNullOrEmpty(newChannelId))
          {
            _logger.LogInformation("New channel {NewChannelId} received from callback. Invoking OnQrCodeAvailable event.", newChannelId);

            if (OnQrCodeAvailable != null)
            {
              await OnQrCodeAvailable.Invoke(newChannelId);
            }
          }
          else
          {
            _logger.LogInformation("Switch channel callback returned no new channel. Skipping event.");
          }
        }
        catch (OperationCanceledException)
        {
          throw;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Failed to execute switch channel callback automatically.");

          if (OnError != null)
            await OnError.Invoke($"Yeni QR kod alınamadı: {ex.Message}");

          return;
        }
      }
    }
    catch (OperationCanceledException)
    {
      _logger.LogInformation("QR refresh timer loop stopped (Task Canceled).");
    }
  }

  public async ValueTask DisposeAsync()
  {
    await StopAsync();
    _refreshCts?.Dispose();
    _refreshCts = null;

    GC.SuppressFinalize(this);
  }
}
