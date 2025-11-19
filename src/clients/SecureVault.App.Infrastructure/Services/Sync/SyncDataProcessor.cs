using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Sync;

public class SyncDataProcessor : ISyncDataProcessor
{
    private readonly IReadOnlyDictionary<ItemType, IEntityDataProcessor> _processors;
    private readonly IStorageService _storageService;
    private readonly ILogger<SyncDataProcessor> _logger;

    public SyncDataProcessor(
        IEnumerable<IEntityDataProcessor> processors,
        IStorageService storageService,
        ILogger<SyncDataProcessor> logger)
    {
        _processors = processors.ToDictionary(p => p.Type);
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<Result> ProcessServerDataAsync(IReadOnlyCollection<VaultItemDto> serverItems)
    {
        _logger.LogInformation("PULL processing starting for {ItemCount} items from server...", serverItems.Count);

        var encryptionKey = await _storageService.GetEncryptionKeyAsByteAsync();
        if (encryptionKey == null)
        {
            _logger.LogError("PULL processing failed: Encryption key could not be retrieved from storage.");
            return Result.Failure(new Error(ErrorCodes.Client.SyncPullNoKey, "Encryption key not found."));
        }

        var processingTasks = new List<Task<Result>>();

        _logger.LogDebug("Delegating items to {ProcessorCount} registered processors.", _processors.Count);

        foreach (var processor in _processors.Values)
        {
            processingTasks.Add(processor.ProcessServerDataAsync(serverItems, encryptionKey));
        }
        try
        {
            var results = await Task.WhenAll(processingTasks);

            var failedResults = results.Where(r => r.IsFailure).ToList();
            if (failedResults.Any())
            {
                _logger.LogError("One or more entity processors failed during PULL sync.");
                foreach (var failure in failedResults)
                {
                    _logger.LogWarning("- Processor Failure: {ErrorCode} - {ErrorMessage}", failure.Error.Code, failure.Error.Message);
                }
                return Result.Failure(failedResults.First().Error);
            }
            _logger.LogInformation("All entity processors completed PULL sync successfully.");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "A critical exception occurred during Task.WhenAll for sync processors. PULL failed.");
            return Result.Failure(new Error(ErrorCodes.Client.SyncPullCriticalError, "A critical, unexpected error occurred while processing server data."));
        }
    }
}
