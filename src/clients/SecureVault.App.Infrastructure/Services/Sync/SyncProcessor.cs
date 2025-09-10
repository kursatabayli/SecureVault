using AutoMapper;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Sync
{
    public class SyncProcessor<TEntity> : ISyncProcessor where TEntity : class, ISynchronizableEntity
    {
        private readonly ILogger<SyncProcessor<TEntity>> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SyncProcessor(ILogger<SyncProcessor<TEntity>> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("{EntityType} için senkronize edilmemiş kayıtlar işleniyor.", typeof(TEntity).Name);

            await using var scope = _serviceProvider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var vaultItemService = scope.ServiceProvider.GetRequiredService<IVaultItemService>();
            var cryptoService = scope.ServiceProvider.GetRequiredService<ICryptoService>();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            var payloadFactory = scope.ServiceProvider.GetRequiredService<IEntityPayloadFactory<TEntity>>();

            var repository = unitOfWork.GetRepository<TEntity>();
            var unsyncedItems = await repository.Find(e => !e.IsSynced, cancellationToken);

            if (!unsyncedItems.Any())
                return;

            _logger.LogInformation("{Count} adet senkronize edilmemiş {EntityType} bulundu.", unsyncedItems.Count(), typeof(TEntity).Name);

            var encryptionKey = await storageService.GetEncryptionKeyAsByteAsync();
            if (encryptionKey is null)
            {
                _logger.LogError("Şifreleme anahtarı alınamadı. {EntityType} senkronizasyonu iptal edildi.", typeof(TEntity).Name);
                return;
            }

            foreach (var item in unsyncedItems)
            {
                try
                {
                    Result remoteResult;
                    var payload = payloadFactory.CreatePayload(item);

                    if (item.IsDeleted)
                    {
                        _logger.LogInformation("Uzak sunucudan siliniyor: {EntityType} ID:{Id}", typeof(TEntity).Name, item.Id);

                        remoteResult = await vaultItemService.DeleteVaultItemAsync(item.Id, cancellationToken);

                        if (remoteResult.IsSuccess)
                        {
                            repository.Remove(item);
                            _logger.LogInformation("Yerel veritabanından kalıcı olarak silindi: {EntityType} ID:{Id}", typeof(TEntity).Name, item.Id);
                        }
                    }
                    else if (item.Version == 1)
                    {
                        _logger.LogInformation("Uzak sunucuda oluşturuluyor: {EntityType} ID:{Id}", typeof(TEntity).Name, item.Id);

                        var encryptedData = cryptoService.Encrypt(payload.DtoToEncrypt, encryptionKey);
                        var createDto = new CreateVaultItemDto
                        {
                            Id = item.Id,
                            ItemType = payload.ItemType,
                            EncryptedData = encryptedData,
                            CreatedAt = item.UpdatedAt,
                        };

                        remoteResult = await vaultItemService.CreateVaultItemAsync(createDto, cancellationToken);

                        if (remoteResult.IsSuccess)
                        {
                            item.MarkAsSynced();
                            _logger.LogInformation("{EntityType} ID:{Id} başarıyla oluşturuldu ve senkronize edildi.", typeof(TEntity).Name, item.Id);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Uzak sunucuda güncelleniyor: {EntityType} ID:{Id}, Version:{Version}", typeof(TEntity).Name, item.Id, item.Version);

                        var dtoToEncrypt = mapper.Map<object>(item);
                        var encryptedData = cryptoService.Encrypt(payload.DtoToEncrypt, encryptionKey);
                        var updateDto = new UpdateVaultItemDto
                        {
                            Id = item.Id,
                            EncryptedData = encryptedData,
                            Version = item.Version,
                            UpdatedAt = item.UpdatedAt,
                        };

                        remoteResult = await vaultItemService.UpdateVaultItemAsync(updateDto, cancellationToken);

                        if (remoteResult.IsSuccess)
                        {
                            item.MarkAsSynced();
                            _logger.LogInformation("{EntityType} ID:{Id} başarıyla güncellendi ve senkronize edildi.", typeof(TEntity).Name, item.Id);
                        }
                    }

                    if (remoteResult is not null && !remoteResult.IsSuccess)
                    {
                        _logger.LogWarning("{EntityType} ID:{Id} senkronize edilemedi: {Error}", typeof(TEntity).Name, item.Id, remoteResult.Error.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{EntityType} ID:{Id} senkronizasyonu sırasında bir hata oluştu.", typeof(TEntity).Name, item.Id);
                }
            }

            await unitOfWork.CompleteAsync();
        }
    }
}
