using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;

namespace SecureVault.App.Infrastructure.Services.Sync;

public class BackgroundSyncService : IBackgroundSyncService
{
    private readonly ILogger<BackgroundSyncService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISyncLock _syncLock;
    public BackgroundSyncService(
        ILogger<BackgroundSyncService> logger,
        IServiceScopeFactory scopeFactory,
        ISyncLock syncLock)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _syncLock = syncLock;
    }

    public async Task<bool> SynchronizeAsync(CancellationToken cancellationToken)
    {
        await using (var lockHandle = await _syncLock.AcquireLockAsync(TimeSpan.Zero, cancellationToken))
        {
            if (!lockHandle.IsAcquired)
            {
                _logger.LogInformation("Another sync operation (Login/Initial) is already running. Background sync is being skipped.");
                return false;
            }

            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    _logger.LogWarning("Synchronization operation skipped (no internet connection).");
                    return false;
                }

                _logger.LogInformation("One-time synchronization operation starting...");

                await using var scope = _scopeFactory.CreateAsyncScope();
                var syncProcessors = scope.ServiceProvider.GetRequiredService<IEnumerable<ISyncProcessor>>();

                _logger.LogInformation("PUSH phase starting...");
                await PushLocalChangesAsync(syncProcessors, cancellationToken);
                _logger.LogInformation("PUSH phase completed.");

                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("PULL phase starting...");
                var pullResult = await PullServerChangesAsync(scope.ServiceProvider, cancellationToken);
                _logger.LogInformation("PULL phase completed with result: {PullResult}", pullResult);

                if (!pullResult)
                {
                    _logger.LogWarning("The PULL step of synchronization failed. Operation marked as failed.");
                    return false;
                }

                _logger.LogInformation("Synchronization operation completed successfully.");
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Synchronization operation was canceled.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during synchronization.");
                return false;
            }
        }
    }

    private async Task PushLocalChangesAsync(IEnumerable<ISyncProcessor> syncProcessors, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Pushing local changes to the server (PUSH)...");
        foreach (var processor in syncProcessors)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("PUSH operation was canceled.");
                break;
            }
            await processor.ProcessAsync(cancellationToken);
        }
    }

    private async Task<bool> PullServerChangesAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Pulling server changes to local (PULL)...");

        var storageService = serviceProvider.GetRequiredService<IStorageService>();
        var vaultItemService = serviceProvider.GetRequiredService<IVaultItemService>();
        var syncDataProcessor = serviceProvider.GetRequiredService<ISyncDataProcessor>();
        var lastSyncTimestamp = storageService.GetLastSyncDate();
        var syncData = (lastSyncTimestamp == DateTimeOffset.MinValue)
            ? await vaultItemService.GetUserVaultAsync(cancellationToken)
            : await vaultItemService.GetVaultItemByLastSyncTimeAsync(lastSyncTimestamp, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (!syncData.IsSuccess)
            {
                _logger.LogWarning("Could not pull updates from the server: {Error}", syncData.Error.Message);
                return false;
            }
            if (syncData.Value is null || !syncData.Value.Any())
            {
                _logger.LogInformation("No new changes found on the server.");
                return true;
            }

            _logger.LogInformation("{Count} records received from the server. Processing local database...", syncData.Value.Count);
            var processingResult = await syncDataProcessor.ProcessServerDataAsync(syncData.Value);

            if (processingResult.IsSuccess)
            {
                var maxUpdatedAt = syncData.Value.Max(x => x.UpdatedAt);
                storageService.SetLastSyncDate(maxUpdatedAt);
                _logger.LogInformation("Local database updated successfully. New synchronization time: {Timestamp}", maxUpdatedAt);
                return true;
            }
            else
            {
                _logger.LogError("An error occurred while processing data from the server: {Error}", processingResult.Error.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A critical error occurred while pulling server updates.");
            return false;
        }
    }
}
