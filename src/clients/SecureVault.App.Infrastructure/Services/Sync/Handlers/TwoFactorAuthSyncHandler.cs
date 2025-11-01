using AutoMapper;
using Microsoft.Extensions.Logging;
using Realms;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.DTOs.Sync;
using SecureVault.App.Application.Contracts.DTOs.TwoFactorAuthCodes;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Sync.Handlers
{
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

        public SyncPayload CreatePayload(TwoFactorAuthCodeEntity entity)
        {
            var dto = _mapper.Map<TwoFactorAuthCodeDto>(entity);
            return new SyncPayload(dto, ItemType.TwoFactorAuth);
        }
        public async Task<Result> ProcessServerDataAsync(IReadOnlyCollection<VaultItemDto> allServerItems, byte[] encryptionKey)
        {
            var myServerItems = allServerItems.Where(i => i.ItemType == Type).ToList();
            if (!myServerItems.Any())
                return Result.Success();
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
                        itemsToDelete.Add(existing);
                        continue;
                    }
                    if (serverItem.IsDeleted) continue;

                    if (existing != null && !existing.IsSynced)
                    {
                        if (existing.UpdatedAt > serverItem.UpdatedAt)
                        {
                            _logger.LogWarning("Çakışma tespit edildi (Lokal kazandı). Sunucudan gelen ID:{Id} atlanıyor.", serverItem.Id);
                            continue;
                        }
                        else
                        {
                            _logger.LogWarning("Çakışma tespit edildi (Sunucu kazandı). Lokal ID:{Id} üzerine yazılıyor.", serverItem.Id);
                        }
                    }

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

                await repository.ApplyServerPullUpsertsAsync(itemsToUpsert);
                await repository.HardDeleteRangeAsync(itemsToDelete);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(new Error("Sync.Pull.TwoFactorAuthHandlerError", ex.Message));
            }
        }

    }
}
