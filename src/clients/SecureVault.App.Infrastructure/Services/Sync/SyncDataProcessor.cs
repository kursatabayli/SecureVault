using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Sync
{
    public class SyncDataProcessor : ISyncDataProcessor
    {
        private readonly IReadOnlyDictionary<ItemType, IEntityDataProcessor> _processors;
        private readonly IStorageService _storageService;

        public SyncDataProcessor(
            IEnumerable<IEntityDataProcessor> processors,
            IStorageService storageService)
        {
            _processors = processors.ToDictionary(p => p.Type);
            _storageService = storageService;
        }

        public async Task<Result> ProcessServerDataAsync(IReadOnlyCollection<VaultItemDto> serverItems)
        {
            var encryptionKey = await _storageService.GetEncryptionKeyAsByteAsync();
            if (encryptionKey == null)
            {
                return Result.Failure(new Error("Sync.Pull.NoKey", "Şifreleme anahtarı bulunamadı."));
            }
            var processingTasks = new List<Task<Result>>();

            foreach (var processor in _processors.Values)
            {
                processingTasks.Add(processor.ProcessServerDataAsync(serverItems, encryptionKey));
            }
            try
            {
                var results = await Task.WhenAll(processingTasks);

                var failedResult = results.FirstOrDefault(r => r.IsFailure);
                if (failedResult != null)
                {
                    return Result.Failure(failedResult.Error);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(new Error("Sync.Pull.CriticalError", ex.Message));
            }

        }
    }
}
