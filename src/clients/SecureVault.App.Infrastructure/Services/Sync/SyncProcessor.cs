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

namespace SecureVault.App.Infrastructure.Services.Sync;

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
        _logger.LogTrace("Processing unsynced records for {EntityType}.", typeof(TEntity).Name);

        var unsyncedItemsQuery = await _repository.Find(e => !e.IsSynced);
        var unsyncedItems = unsyncedItemsQuery.ToList();

        if (!unsyncedItems.Any())
        {
            _logger.LogInformation("No unsynced {EntityType} items found to push.", typeof(TEntity).Name);
            return;
        }

        _logger.LogInformation("Found {Count} unsynced {EntityType} items to push.", unsyncedItems.Count(), typeof(TEntity).Name);

        var encryptionKey = await _storageService.GetEncryptionKeyAsByteAsync();
        if (encryptionKey is null)
        {
            _logger.LogError("Failed to retrieve encryption key. {EntityType} synchronization (push) cancelled.", typeof(TEntity).Name);
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
                    _logger.LogInformation("Pushing DELETE to remote server: {EntityType} ID:{Id}", typeof(TEntity).Name, item.Id);
                    itemsToDelete.Add(item.Id);
                    localItemsToDelete.Add(item);
                }
                else if (item.Version == 1)
                {
                    _logger.LogInformation("Pushing CREATE to remote server: {EntityType} ID:{Id}", typeof(TEntity).Name, item.Id);

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
                    _logger.LogInformation("Pushing UPDATE to remote server: {EntityType} ID:{Id}, Version:{Version}", typeof(TEntity).Name, item.Id, item.Version);

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
                _logger.LogError(ex, "An error occurred while processing {EntityType} ID:{Id} for synchronization.", typeof(TEntity).Name, item.Id);
            }
        }
        var createResult = itemsToCreate.Any() ? await _vaultItemService.CreateVaultItemsAsync(itemsToCreate, cancellationToken) : Result.Success();
        var updateResult = itemsToUpdate.Any() ? await _vaultItemService.UpdateVaultItemsAsync(itemsToUpdate, cancellationToken) : Result.Success();
        var deleteResult = itemsToDelete.Any() ? await _vaultItemService.DeleteVaultItemsAsync(itemsToDelete, cancellationToken) : Result.Success();

        if (createResult.IsSuccess && updateResult.IsSuccess && deleteResult.IsSuccess)
        {
            _logger.LogInformation("API Push successful. Applying local DB changes ({MarkedSyncedCount} items, {DeletedCount} items)...",
                localItemsToMarkSynced.Count, localItemsToDelete.Count);

            try
            {
                await _repository.ApplyLocalPushChangesAsync(localItemsToMarkSynced, localItemsToDelete);
                _logger.LogInformation("PUSH operation for {EntityType} completed successfully.", typeof(TEntity).Name);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "CRITICAL: API Push succeeded but local DB update failed! Data is now inconsistent and may re-sync. {EntityType}", typeof(TEntity).Name);
            }
        }
        else
        {
            _logger.LogError("PUSH operation failed due to API errors. Errors: C:{CreateError}, U:{UpdateError}, D:{DeleteError}",
                createResult.Error?.Message, updateResult.Error?.Message, deleteResult.Error?.Message);
        }
    }
}
