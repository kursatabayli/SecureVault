using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.DTOs.Passwords;
using SecureVault.App.Application.Contracts.DTOs.TwoFactorAuthCodes;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Services
{
    public class LocalVaultService : ILocalVaultService
    {
        private readonly IVaultItemService _vaultItemService;
        private readonly ICryptoService _cryptoService;
        private readonly ILogger<LocalVaultService> _logger;
        private readonly IStorageService _storageService;
        private readonly IUnitOfWork _unitOfWork;

        public LocalVaultService(IVaultItemService vaultItemService, ICryptoService cryptoService, ILogger<LocalVaultService> logger, IStorageService storageService, IUnitOfWork unitOfWork)
        {
            _vaultItemService = vaultItemService;
            _cryptoService = cryptoService;
            _logger = logger;
            _storageService = storageService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> SetAllVaultDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Sunucudan tüm kasa verileri çekiliyor...");
                var encryptedVault = await _vaultItemService.GetUserVaultAsync(cancellationToken);
                if (encryptedVault.IsFailure)
                {
                    _logger.LogError("Kasa verileri çekilemedi: {Error}", encryptedVault.Error);
                    return Result.Failure(encryptedVault.Error);
                }
                var encryptionKey = await _storageService.GetEncryptionKeyAsByteAsync();
                List<PasswordEntity> passwords = [];
                List<TwoFactorAuthCodeEntity> twoFactorAuths = [];
                foreach (var item in encryptedVault.Value)
                {
                    try
                    {
                        switch (item.ItemType)
                        {
                            case ItemType.Password:
                                var decryptedPasswordData = _cryptoService.Decrypt<PasswordDto>(item.EncryptedData, encryptionKey);
                                var password = MapItemToPasswordEntity(item, decryptedPasswordData);
                                passwords.Add(password);
                                break;
                            case ItemType.TwoFactorAuth:
                                var decryptTwoFactorAuthData = _cryptoService.Decrypt<TwoFactorAuthCodeDto>(item.EncryptedData, encryptionKey);
                                var twoFactorAuth = MapItemToTwoFactorAuthEntity(item, decryptTwoFactorAuthData);
                                twoFactorAuths.Add(twoFactorAuth);
                                break;
                            default:
                                _logger.LogWarning("Bilinmeyen item türü: {ItemType}", item.ItemType);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Bir vault item'ın şifresi çözülemedi. ItemId: {ItemId}", item.Id);
                    }
                }
                _logger.LogInformation("Veriler lokal veritabanına yazılıyor...");

                if (encryptedVault.Value.Count != 0)
                {
                    var mostRecentItem = encryptedVault.Value.MaxBy(item => item.UpdatedAt);
                    _storageService.SetLastSyncDate(mostRecentItem.UpdatedAt);
                }

                if (passwords.Count != 0)
                    await _unitOfWork.Passwords.AddRangeAsync(passwords);

                if (twoFactorAuths.Count != 0)
                    await _unitOfWork.TwoFactorAuthCodes.AddRangeAsync(twoFactorAuths);

                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Tüm kasa verileri başarıyla yüklendi. Toplam şifreler: {PasswordCount}, Toplam 2FA: {TwoFactorAuthCount}", passwords.Count, twoFactorAuths.Count);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm kasa verileri yüklenirken beklenmeyen bir hata oluştu.");
                return Result.Failure(new Error("Client.LoadFailed", "Veriler yüklenemedi."));
            }
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
