using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.Repositories;

namespace SecureVault.App.Infrastructure.Services.Sync
{
    public class BackgroundSyncService : IBackgroundSyncService
    {
        private readonly ILogger<BackgroundSyncService> _logger;
        private readonly IEnumerable<ISyncProcessor> _syncProcessors;
        private readonly IServiceProvider _serviceProvider;

        public BackgroundSyncService(
            ILogger<BackgroundSyncService> logger,
            IEnumerable<ISyncProcessor> syncProcessors,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _syncProcessors = syncProcessors;
            _serviceProvider = serviceProvider;
        }

        public async Task SynchronizeAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Tek seferlik senkronizasyon işlemi başlatılıyor...");

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                _logger.LogWarning("İnternet bağlantısı olmadığından senkronizasyon işlemi atlandı.");
                return;
            }

            try
            {
                await PushLocalChangesAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested) return;

                await PullServerChangesAsync(cancellationToken);

                _logger.LogInformation("Senkronizasyon işlemi başarıyla tamamlandı.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Senkronizasyon işlemi iptal edildi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Senkronizasyon sırasında beklenmedik bir hata oluştu.");
            }
        }

        private async Task PushLocalChangesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Lokal değişiklikler sunucuya gönderiliyor (PUSH)...");
            foreach (var processor in _syncProcessors)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("PUSH işlemi iptal edildi.");
                    break;
                }
                await processor.ProcessAsync(cancellationToken);
            }
        }

        private async Task PullServerChangesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sunucudaki değişiklikler lokale çekiliyor (PULL)...");

            await using var scope = _serviceProvider.CreateAsyncScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var vaultItemService = scope.ServiceProvider.GetRequiredService<IVaultItemService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var syncDataProcessor = scope.ServiceProvider.GetRequiredService<ISyncDataProcessor>();

            try
            {
                var lastSyncTimestamp = storageService.GetLastSyncDate();
                var syncData = (lastSyncTimestamp == DateTimeOffset.MinValue)
                    ? await vaultItemService.GetUserVaultAsync(cancellationToken)
                    : await vaultItemService.GetVaultItemByLastSyncTimeAsync(lastSyncTimestamp, cancellationToken);

                if (cancellationToken.IsCancellationRequested) return;

                if (!syncData.IsSuccess)
                {
                    _logger.LogWarning("Sunucudan güncellemeler çekilemedi: {Error}", syncData.Error.Message);
                    return;
                }
                if (syncData.Value is null || !syncData.Value.Any())
                {
                    _logger.LogInformation("Sunucuda yeni bir değişiklik bulunmuyor.");
                    return;
                }

                _logger.LogInformation("{Count} adet kayıt sunucudan alındı. Lokal veritabanı işleniyor...", syncData.Value.Count);
                var processingResult = await syncDataProcessor.ProcessServerDataAsync(syncData.Value, unitOfWork);

                if (processingResult.IsSuccess)
                {
                    var maxUpdatedAt = syncData.Value.Max(x => x.UpdatedAt);
                    storageService.SetLastSyncDate(maxUpdatedAt);
                    _logger.LogInformation("Lokal veritabanı başarıyla güncellendi. Yeni senkronizasyon zamanı: {Timestamp}", maxUpdatedAt);
                }
                else
                {
                    _logger.LogError("Sunucudan gelen veriler işlenirken hata oluştu: {Error}", processingResult.Error.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sunucu güncellemeleri çekilirken kritik bir hata oluştu.");
            }
        }
    }
}
