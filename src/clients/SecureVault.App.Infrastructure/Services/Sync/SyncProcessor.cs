using AutoMapper;
using Microsoft.Extensions.Logging;
using Realms;
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
    public class SyncProcessor<TEntity> : ISyncProcessor where TEntity : class, ISynchronizableEntity, IRealmObject, new()
    {
        private readonly ILogger<SyncProcessor<TEntity>> _logger;
        private readonly IRepository<TEntity> _repository;
        private readonly IVaultItemService _vaultItemService;
        private readonly ICryptoService _cryptoService;
        private readonly IStorageService _storageService;
        private readonly IEntityPayloadFactory<TEntity> _payloadFactory;

        public SyncProcessor(ILogger<SyncProcessor<TEntity>> logger, IRepository<TEntity> repository, IVaultItemService vaultItemService, ICryptoService cryptoService, IStorageService storageService, IEntityPayloadFactory<TEntity> payloadFactory)
        {
            _logger = logger;
            _repository = repository;
            _vaultItemService = vaultItemService;
            _cryptoService = cryptoService;
            _storageService = storageService;
            _payloadFactory = payloadFactory;
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("{EntityType} için senkronize edilmemiş kayıtlar işleniyor.", typeof(TEntity).Name);

            var unsyncedItemsQuery = await _repository.Find(e => !e.IsSynced);
            var unsyncedItems = unsyncedItemsQuery.ToList();

            if (!unsyncedItems.Any())
                return;

            _logger.LogInformation("{Count} adet senkronize edilmemiş {EntityType} bulundu.", unsyncedItems.Count(), typeof(TEntity).Name);

            var encryptionKey = await _storageService.GetEncryptionKeyAsByteAsync();
            if (encryptionKey is null)
            {
                _logger.LogError("Şifreleme anahtarı alınamadı. {EntityType} senkronizasyonu iptal edildi.", typeof(TEntity).Name);
                return;
            }


            var itemsToCreate = new List<CreateVaultItemDto>();
            var itemsToUpdate = new List<UpdateVaultItemDto>();
            var itemsToDelete = new List<Guid>();

            var localItemsToMarkSynced = new List<TEntity>();
            var localItemsToDelete = new List<TEntity>();

            foreach (var item in unsyncedItems)
            {
                try
                {
                    var payload = _payloadFactory.CreatePayload(item);

                    if (item.IsDeleted)
                    {
                        itemsToDelete.Add(item.Id);
                        localItemsToDelete.Add(item);
                    }
                    else if (item.Version == 1)
                    {
                        _logger.LogInformation("Uzak sunucuda oluşturuluyor: {EntityType} ID:{Id}", typeof(TEntity).Name, item.Id);

                        var encryptedData = _cryptoService.Encrypt(payload.DtoToEncrypt, encryptionKey);
                        var createDto = new CreateVaultItemDto
                        {
                            Id = item.Id,
                            ItemType = payload.ItemType,
                            EncryptedData = encryptedData,
                            CreatedAt = item.UpdatedAt,
                        };
                        itemsToCreate.Add(createDto);
                        localItemsToMarkSynced.Add(item);
                    }
                    else
                    {
                        _logger.LogInformation("Uzak sunucuda güncelleniyor: {EntityType} ID:{Id}, Version:{Version}", typeof(TEntity).Name, item.Id, item.Version);

                        var encryptedData = _cryptoService.Encrypt(payload.DtoToEncrypt, encryptionKey);
                        var updateDto = new UpdateVaultItemDto
                        {
                            Id = item.Id,
                            EncryptedData = encryptedData,
                            Version = item.Version,
                            UpdatedAt = item.UpdatedAt,
                        };

                        itemsToUpdate.Add(updateDto);
                        localItemsToMarkSynced.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{EntityType} ID:{Id} senkronizasyonu sırasında bir hata oluştu.", typeof(TEntity).Name, item.Id);
                }
            }
            var createResult = itemsToCreate.Any() ? await _vaultItemService.CreateVaultItemsAsync(itemsToCreate, cancellationToken) : Result.Success();
            var updateResult = itemsToUpdate.Any() ? await _vaultItemService.UpdateVaultItemsAsync(itemsToUpdate, cancellationToken) : Result.Success();
            var deleteResult = itemsToDelete.Any() ? await _vaultItemService.DeleteVaultItemsAsync(itemsToDelete, cancellationToken) : Result.Success();

            if (createResult.IsSuccess && updateResult.IsSuccess && deleteResult.IsSuccess)
            {
                await _repository.ApplyLocalPushChangesAsync(localItemsToMarkSynced, localItemsToDelete);
                _logger.LogInformation("{EntityType} için PUSH işlemi başarıyla tamamlandı.", typeof(TEntity).Name);
            }
            else
            {
                _logger.LogError("PUSH işlemi sırasında API hatası. Hatalar: C:{CreateError}, U:{UpdateError}, D:{DeleteError}",
                    createResult.Error?.Message, updateResult.Error?.Message, deleteResult.Error?.Message);
            }
        }
    }
}
