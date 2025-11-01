using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;

namespace SecureVault.App.Infrastructure.Services.Sync
{
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
            if (!await _syncLock.WaitAsync(TimeSpan.Zero, cancellationToken))
            {
                _logger.LogInformation("Başka bir senkronizasyon işlemi (Login/Initial) zaten çalışıyor. Arka plan senkronizasyonu atlanıyor.");
                return false;
            }

            try
            {
                _logger.LogInformation("Tek seferlik senkronizasyon işlemi başlatılıyor...");

                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    _logger.LogWarning("İnternet bağlantısı olmadığından senkronizasyon işlemi atlandı.");
                    return false;
                }

                await using var scope = _scopeFactory.CreateAsyncScope();
                var syncProcessors = scope.ServiceProvider.GetRequiredService<IEnumerable<ISyncProcessor>>();
                await PushLocalChangesAsync(syncProcessors, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                var pullResult = await PullServerChangesAsync(scope.ServiceProvider, cancellationToken);

                if (!pullResult)
                {
                    _logger.LogWarning("Senkronizasyonun PULL adımı tamamlanamadı. İşlem başarısız.");
                    return false;
                }

                _logger.LogInformation("Senkronizasyon işlemi başarıyla tamamlandı.");
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Senkronizasyon işlemi iptal edildi.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Senkronizasyon sırasında beklenmedik bir hata oluştu.");
                return false;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private async Task PushLocalChangesAsync(IEnumerable<ISyncProcessor> syncProcessors, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Lokal değişiklikler sunucuya gönderiliyor (PUSH)...");
            foreach (var processor in syncProcessors)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("PUSH işlemi iptal edildi.");
                    break;
                }
                await processor.ProcessAsync(cancellationToken);
            }
        }

        private async Task<bool> PullServerChangesAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sunucudaki değişiklikler lokale çekiliyor (PULL)...");

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
                    _logger.LogWarning("Sunucudan güncellemeler çekilemedi: {Error}", syncData.Error.Message);
                    return false;
                }
                if (syncData.Value is null || !syncData.Value.Any())
                {
                    _logger.LogInformation("Sunucuda yeni bir değişiklik bulunmuyor.");
                    return true;
                }

                _logger.LogInformation("{Count} adet kayıt sunucudan alındı. Lokal veritabanı işleniyor...", syncData.Value.Count);
                var processingResult = await syncDataProcessor.ProcessServerDataAsync(syncData.Value);

                if (processingResult.IsSuccess)
                {
                    var maxUpdatedAt = syncData.Value.Max(x => x.UpdatedAt);
                    storageService.SetLastSyncDate(maxUpdatedAt);
                    _logger.LogInformation("Lokal veritabanı başarıyla güncellendi. Yeni senkronizasyon zamanı: {Timestamp}", maxUpdatedAt);
                    return true;
                }
                else
                {
                    _logger.LogError("Sunucudan gelen veriler işlenirken hata oluştu: {Error}", processingResult.Error.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sunucu güncellemeleri çekilirken kritik bir hata oluştu.");
                return false;
            }
        }
    }
}
