using AutoMapper;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.DTOs.Sync;
using SecureVault.App.Application.Contracts.DTOs.TwoFactorAuthCodes;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Sync.Handlers;

public class TwoFactorAuthSyncHandler : IEntityPayloadFactory<TwoFactorAuthCodeEntity>, IEntityDataProcessor
{
    private readonly IMapper _mapper;
    private readonly ICryptoService _cryptoService;
    private readonly ILogger<TwoFactorAuthSyncHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public TwoFactorAuthSyncHandler(
        IMapper mapper,
        ICryptoService cryptoService,
        ILogger<TwoFactorAuthSyncHandler> logger,
        IServiceScopeFactory scopeFactory)
    {
        _mapper = mapper;
        _cryptoService = cryptoService;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public ItemType Type => ItemType.TwoFactorAuth;

    public async Task<Result> ProcessServerDataAsync(IReadOnlyCollection<VaultItemDto> allServerItems, byte[] encryptionKey)
    {
        var myServerItems = allServerItems.Where(i => i.ItemType == Type).ToList();

        if (!myServerItems.Any())
        {
            _logger.LogInformation("PULL: No '{ItemType}' items found in server data.", Type);
            return Result.Success();
        }

        _logger.LogInformation("PULL: Processing {ItemCount} '{ItemType}' items from server...", myServerItems.Count, Type);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var repository = scope.ServiceProvider.GetRequiredService<ITwoFactorAuthCodeRepository>();

            var allMyIds = myServerItems.Select(i => i.Id).ToList();
            var allLocalItems = await repository.GetAllAsync();
            var localItemsDict = allLocalItems.Where(p => allMyIds.Contains(p.Id)).ToDictionary(p => p.Id);

            var itemsToUpsert = new List<TwoFactorAuthCodeEntity>();
            var itemsToDelete = new List<TwoFactorAuthCodeEntity>();

            foreach (var serverItem in myServerItems)
            {
                localItemsDict.TryGetValue(serverItem.Id, out var existing);

                if (serverItem.IsDeleted && existing != null)
                {
                    _logger.LogInformation("PULL: Deleting local {ItemType} ID: {Id} (server version deleted).", Type, existing.Id);
                    itemsToDelete.Add(existing);
                    continue;
                }
                if (serverItem.IsDeleted) continue;

                if (existing != null && !existing.IsSynced)
                {
                    if (existing.UpdatedAt > serverItem.UpdatedAt)
                    {
                        _logger.LogWarning("CONFLICT (Local Wins): Server {ItemType} ID:{Id} was skipped.", Type, serverItem.Id);
                        continue;
                    }
                    else
                    {
                        _logger.LogWarning("CONFLICT (Server Wins): Local {ItemType} ID:{Id} will be overwritten.", Type, serverItem.Id);
                    }
                }

                _logger.LogInformation("PULL: Upserting local {ItemType} ID: {Id} (Server Version: {Version})", Type, serverItem.Id, serverItem.Version);

                var decryptedDto = _cryptoService.Decrypt<TwoFactorAuthCodeDto>(serverItem.EncryptedData, encryptionKey);
                var entityToUpsert = new TwoFactorAuthCodeEntity
                {
                    Id = serverItem.Id,
                    Issuer = decryptedDto.Issuer,
                    AccountName = decryptedDto.AccountName,
                    SecretKey = decryptedDto.SecretKey,
                    Type = decryptedDto.Type,
                    Digits = decryptedDto.Digits,
                    Period = decryptedDto.Period,
                    Counter = decryptedDto.Counter,
                    Algorithm = decryptedDto.Algorithm,
                    Version = serverItem.Version,
                    CreatedAt = serverItem.CreatedAt,
                    UpdatedAt = serverItem.UpdatedAt,
                };
                entityToUpsert.MarkAsSynced();
                itemsToUpsert.Add(entityToUpsert);
            }

            _logger.LogInformation("PULL: Applying local DB changes for {ItemType}. Upserting: {UpsertCount}, Deleting: {DeleteCount}",
                                Type, itemsToUpsert.Count, itemsToDelete.Count);

            await repository.ApplyServerPullUpsertsAsync(itemsToUpsert);
            await repository.HardDeleteRangeAsync(itemsToDelete);

            _logger.LogInformation("PULL: Successfully processed {ItemType} server data.", Type);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PULL: Failed to process {ItemType} data.", Type);
            return Result.Failure(new Error(ErrorCodes.Client.SyncPull, "An unexpected error occurred while processing two-factor authentication codes."));
        }
    }
    public SyncPayload CreatePayload(TwoFactorAuthCodeEntity entity)
    {
        var dto = _mapper.Map<TwoFactorAuthCodeDto>(entity);
        return new SyncPayload(dto, ItemType.TwoFactorAuth);
    }
}
