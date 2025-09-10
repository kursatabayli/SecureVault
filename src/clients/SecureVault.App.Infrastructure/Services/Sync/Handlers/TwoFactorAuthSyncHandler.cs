using AutoMapper;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.DTOs.Sync;
using SecureVault.App.Application.Contracts.DTOs.TwoFactorAuthCodes;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;

namespace SecureVault.App.Infrastructure.Services.Sync.Handlers
{
    public class TwoFactorAuthSyncHandler : IEntityPayloadFactory<TwoFactorAuthCodeEntity>, IEntityDataProcessor
    {
        private readonly IMapper _mapper;
        private readonly ICryptoService _cryptoService;
        private readonly IStorageService _storageService;

        public TwoFactorAuthSyncHandler(IMapper mapper, ICryptoService cryptoService, IStorageService storageService)
        {
            _mapper = mapper;
            _cryptoService = cryptoService;
            _storageService = storageService;
        }
        public ItemType Type => ItemType.TwoFactorAuth;

        public SyncPayload CreatePayload(TwoFactorAuthCodeEntity entity)
        {
            var dto = _mapper.Map<TwoFactorAuthCodeDto>(entity);
            return new SyncPayload(dto, ItemType.TwoFactorAuth);
        }

        public async Task ProcessItemAsync(IUnitOfWork unitOfWork, VaultItemDto serverItem)
        {
            var repo = unitOfWork.GetRepository<TwoFactorAuthCodeEntity>();
            var existing = await repo.GetByIdAsync(serverItem.Id);
            var encryptionKey = await _storageService.GetEncryptionKeyAsByteAsync();

            if (serverItem.IsDeleted && existing != null)
            {
                repo.Remove(existing);
                return;
            }

            var decryptedDto = _cryptoService.Decrypt<TwoFactorAuthCodeDto>(serverItem.EncryptedData, encryptionKey);


            if (existing == null)
            {
                var newEntity = MapItemToTwoFactorAuthEntity(serverItem, decryptedDto);
                newEntity.MarkAsSynced();
                await repo.AddAsync(newEntity);
            }
            else if (serverItem.Version > existing.Version)
            {
                existing.Update(
                    decryptedDto.Issuer,
                    decryptedDto.AccountName,
                    decryptedDto.Counter,
                    serverItem.Version,
                    serverItem.UpdatedAt
                    );
                existing.MarkAsSynced();
                repo.Update(existing);
            }
        }

        private static TwoFactorAuthCodeEntity MapItemToTwoFactorAuthEntity(VaultItemDto item, TwoFactorAuthCodeDto twoFactorAuthModel)
        {
            return TwoFactorAuthCodeEntity.Create(
                item.Id,
                twoFactorAuthModel.Issuer,
                twoFactorAuthModel.AccountName,
                twoFactorAuthModel.SecretKey,
                twoFactorAuthModel.Type,
                twoFactorAuthModel.Digits,
                twoFactorAuthModel.Period,
                twoFactorAuthModel.Counter,
                twoFactorAuthModel.Algorithm,
                item.Version,
                item.CreatedAt,
                item.UpdatedAt
            );
        }
    }
}
