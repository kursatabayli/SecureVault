using AutoMapper;
using Microsoft.Extensions.Logging;
using Realms;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.DTOs.Passwords;
using SecureVault.App.Application.Contracts.DTOs.Sync;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Sync.Handlers
{
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
                return Result.Success();

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

                await repository.ApplyServerPullUpsertsAsync(itemsToUpsert);
                await repository.HardDeleteRangeAsync(itemsToDelete);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(new Error("Sync.Pull.PasswordHandlerError", ex.Message));
            }
        }
        public SyncPayload CreatePayload(PasswordEntity entity)
        {
            var dto = _mapper.Map<PasswordDto>(entity);
            return new SyncPayload(dto, ItemType.Password);
        }
    }
}
