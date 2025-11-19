using AutoMapper;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.DTOs.Passwords;
using SecureVault.App.Application.Contracts.DTOs.Sync;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Sync.Handlers;

public class PasswordSyncHandler : IEntityPayloadFactory<PasswordEntity>, IEntityDataProcessor
{
    private readonly IMapper _mapper;
    private readonly ICryptoService _cryptoService;
    private readonly ILogger<PasswordSyncHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public PasswordSyncHandler(
        IMapper mapper,
        ICryptoService cryptoService,
        ILogger<PasswordSyncHandler> logger,
        IServiceScopeFactory scopeFactory)
    {
        _mapper = mapper;
        _cryptoService = cryptoService;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public ItemType Type => ItemType.Password;
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

            var repository = scope.ServiceProvider.GetRequiredService<IPasswordRepository>();

            var allMyIds = myServerItems.Select(i => i.Id).ToList();
            var allLocalItems = await repository.GetAllAsync();
            var localItemsDict = allLocalItems.Where(p => allMyIds.Contains(p.Id)).ToDictionary(p => p.Id);

            var itemsToUpsert = new List<PasswordEntity>();
            var itemsToDelete = new List<PasswordEntity>();

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

                var decryptedDto = _cryptoService.Decrypt<PasswordDto>(serverItem.EncryptedData, encryptionKey);
                var entityToUpsert = new PasswordEntity
                {
                    Id = serverItem.Id,
                    SiteName = decryptedDto.SiteName,
                    SiteUrl = decryptedDto.SiteUrl,
                    Username = decryptedDto.Username,
                    Password = decryptedDto.Password,
                    Notes = decryptedDto.Notes,
                    Version = serverItem.Version,
                    CreatedAt = serverItem.CreatedAt,
                    UpdatedAt = serverItem.UpdatedAt
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
            return Result.Failure(new Error(ErrorCodes.Client.SyncPull, "An unexpected error occurred while processing passwords."));
        }
    }
    public SyncPayload CreatePayload(PasswordEntity entity)
    {
        var dto = _mapper.Map<PasswordDto>(entity);
        return new SyncPayload(dto, ItemType.Password);
    }
}
