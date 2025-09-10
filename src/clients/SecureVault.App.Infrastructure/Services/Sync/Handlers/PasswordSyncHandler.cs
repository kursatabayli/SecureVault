using AutoMapper;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.DTOs.Passwords;
using SecureVault.App.Application.Contracts.DTOs.Sync;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;

namespace SecureVault.App.Infrastructure.Services.Sync.Handlers
{
    public class PasswordSyncHandler : IEntityPayloadFactory<PasswordEntity>, IEntityDataProcessor
    {
        private readonly IMapper _mapper;
        private readonly ICryptoService _cryptoService;
        private readonly IStorageService _storageService;

        public PasswordSyncHandler(IMapper mapper, ICryptoService cryptoService, IStorageService storageService)
        {
            _mapper = mapper;
            _cryptoService = cryptoService;
            _storageService = storageService;
        }

        public ItemType Type => ItemType.Password;

        public async Task ProcessItemAsync(IUnitOfWork unitOfWork, VaultItemDto serverItem)
        {
            var repo = unitOfWork.GetRepository<PasswordEntity>();
            var existing = await repo.GetByIdAsync(serverItem.Id);
            var encryptionKey = await _storageService.GetEncryptionKeyAsByteAsync();

            if (serverItem.IsDeleted && existing != null)
            {
                repo.Remove(existing);
                return;
            }

            var decryptedDto = _cryptoService.Decrypt<PasswordDto>(serverItem.EncryptedData, encryptionKey);


            if (existing == null)
            {
                var newEntity = MapItemToPasswordEntity(serverItem, decryptedDto);
                newEntity.MarkAsSynced();
                await repo.AddAsync(newEntity);
            }
            else if (serverItem.Version > existing.Version)
            {
                existing.Update(
                    decryptedDto.SiteName,
                    decryptedDto.SiteUrl,
                    decryptedDto.Username,
                    decryptedDto.Password,
                    decryptedDto.Notes,
                    serverItem.Version,
                    serverItem.UpdatedAt
                    );
                existing.MarkAsSynced();
                repo.Update(existing);
            }
        }

        public SyncPayload CreatePayload(PasswordEntity entity)
        {
            var dto = _mapper.Map<PasswordDto>(entity);
            return new SyncPayload(dto, ItemType.Password);
        }

        private static PasswordEntity MapItemToPasswordEntity(VaultItemDto item, PasswordDto passwordModel)
        {
            return PasswordEntity.Create(
                item.Id,
                passwordModel.SiteName,
                passwordModel.SiteUrl,
                passwordModel.Username,
                passwordModel.Password,
                passwordModel.Notes,
                item.Version,
                item.CreatedAt,
                item.UpdatedAt
            );
        }
    }
}
