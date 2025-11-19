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

namespace SecureVault.App.Application.Services;

public class LocalVaultService : ILocalVaultService
{
    private readonly IVaultItemService _vaultItemService;
    private readonly ICryptoService _cryptoService;
    private readonly ILogger<LocalVaultService> _logger;
    private readonly IStorageService _storageService;
    private readonly IPasswordRepository _passwordRepository;
    private readonly ITwoFactorAuthCodeRepository _twoFactorAuthCodeRepository;
    public LocalVaultService(
        IVaultItemService vaultItemService,
        ICryptoService cryptoService,
        ILogger<LocalVaultService> logger,
        IStorageService storageService,
        IPasswordRepository passwordRepository,
        ITwoFactorAuthCodeRepository twoFactorAuthCodeRepository)
    {
        _vaultItemService = vaultItemService;
        _cryptoService = cryptoService;
        _logger = logger;
        _storageService = storageService;
        _passwordRepository = passwordRepository;
        _twoFactorAuthCodeRepository = twoFactorAuthCodeRepository;
    }

    public async Task<Result> SetAllVaultDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting pull of all vault data from server...");
            var encryptedVault = await _vaultItemService.GetUserVaultAsync(cancellationToken);

            if (encryptedVault.IsFailure)
            {
                _logger.LogError("Failed to retrieve vault data from API: {Error}", encryptedVault.Error);
                return Result.Failure(encryptedVault.Error);
            }

            var encryptionKey = await _storageService.GetEncryptionKeyAsByteAsync();

            if (encryptionKey == null)
            {
                _logger.LogCritical("Cannot decrypt vault data: Encryption key not found in storage.");
                return Result.Failure(new Error("Client.NoKey", "Encryption key not available on this device. Cannot load vault data."));
            }

            _logger.LogInformation("Decrypting {Count} vault items in background thread...", encryptedVault.Value.Count);

            var (passwords, twoFactorAuths) = await Task.Run(() =>
            {
                List<PasswordEntity> pList = [];
                List<TwoFactorAuthCodeEntity> tList = [];

                foreach (var item in encryptedVault.Value)
                {
                    try
                    {
                        switch (item.ItemType)
                        {
                            case ItemType.Password:
                                var decryptedPasswordData = _cryptoService.Decrypt<PasswordDto>(item.EncryptedData, encryptionKey);
                                var password = MapItemToPasswordEntity(item, decryptedPasswordData);
                                password.MarkAsSynced();
                                pList.Add(password);
                                break;
                            case ItemType.TwoFactorAuth:
                                var decryptTwoFactorAuthData = _cryptoService.Decrypt<TwoFactorAuthCodeDto>(item.EncryptedData, encryptionKey);
                                var twoFactorAuth = MapItemToTwoFactorAuthEntity(item, decryptTwoFactorAuthData);
                                twoFactorAuth.MarkAsSynced();
                                tList.Add(twoFactorAuth);
                                break;
                            default:
                                _logger.LogWarning("Skipping unknown item type: {ItemType}", item.ItemType);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to decrypt/deserialize vault item ID: {ItemId}. Skipping item.", item.Id);
                    }
                }

                return (pList, tList);
            }, cancellationToken);

            _logger.LogInformation("Decryption complete. Writing data to local database...");

            if (encryptedVault.Value.Count != 0)
            {
                var mostRecentItem = encryptedVault.Value.MaxBy(item => item.UpdatedAt);
                _storageService.SetLastSyncDate(mostRecentItem!.UpdatedAt);
            }

            if (passwords.Count > 0)
            {
                _logger.LogDebug("Saving {Count} Password entities...", passwords.Count);
                await _passwordRepository.AddRangeAsync(passwords);
            }
            if (twoFactorAuths.Count > 0)
            {
                _logger.LogDebug("Saving {Count} 2FA Code entities...", twoFactorAuths.Count);
                await _twoFactorAuthCodeRepository.AddRangeAsync(twoFactorAuths);
            }

            _logger.LogInformation("All vault data successfully loaded. Passwords: {PasswordCount}, 2FA Codes: {TwoFactorAuthCount}", passwords.Count, twoFactorAuths.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while loading all vault data.");
            return Result.Failure(new Error("Client.LoadFailed", "Failed to load vault data."));
        }
    }


    private static PasswordEntity MapItemToPasswordEntity(VaultItemDto item, PasswordDto passwordModel)
    {
        return new PasswordEntity
        {
            Id = item.Id,
            SiteName = passwordModel.SiteName,
            SiteUrl = passwordModel.SiteUrl,
            Username = passwordModel.Username,
            Password = passwordModel.Password,
            Notes = passwordModel.Notes,
            Version = item.Version,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    private static TwoFactorAuthCodeEntity MapItemToTwoFactorAuthEntity(VaultItemDto item, TwoFactorAuthCodeDto twoFactorAuthModel)
    {
        return new TwoFactorAuthCodeEntity
        {
            Id = item.Id,
            Issuer = twoFactorAuthModel.Issuer,
            AccountName = twoFactorAuthModel.AccountName,
            SecretKey = twoFactorAuthModel.SecretKey,
            Type = twoFactorAuthModel.Type,
            Digits = twoFactorAuthModel.Digits,
            Period = twoFactorAuthModel.Period,
            Counter = twoFactorAuthModel.Counter,
            Algorithm = twoFactorAuthModel.Algorithm,
            Version = item.Version,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
