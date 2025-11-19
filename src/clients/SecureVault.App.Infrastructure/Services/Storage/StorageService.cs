using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Infrastructure.Helpers.Constants;
using System.Security.Cryptography;

namespace SecureVault.App.Infrastructure.Services.Storage;

public class StorageService : IStorageService
{
    private readonly IPreferences _preferences;
    private readonly ISecureStorage _secureStorage;
    private readonly ILogger<StorageService> _logger;

    public StorageService(IPreferences preferences, ISecureStorage secureStorage, ILogger<StorageService> logger)
    {
        _preferences = preferences;
        _secureStorage = secureStorage;
        _logger = logger;
    }

    private async Task<string?> GetSecureAsync(string key)
    {
        try
        {
            return await _secureStorage.GetAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve key '{Key}' from SecureStorage.", key);
            return null;
        }
    }

    private T GetPreference<T>(string key, T defaultValue)
    {
        try
        {
            return _preferences.Get(key, defaultValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get preference key '{Key}'. Returning default.", key);
            return defaultValue;
        }
    }

    public async Task<string?> GetAccessTokenAsync() => await GetSecureAsync(StorageItems.AccessToken);
    public async Task<string?> GetRefreshTokenAsync() => await GetSecureAsync(StorageItems.RefreshToken);
    public async Task<string?> GetPrivateKeyAsync() => await GetSecureAsync(StorageItems.PrivateKey);
    public async Task<byte[]?> GetEncryptionKeyAsByteAsync()
    {
        try
        {
            var encryptionKeyHex = await _secureStorage.GetAsync(StorageItems.EncryptionKey);
            if (string.IsNullOrEmpty(encryptionKeyHex))
            {
                _logger.LogWarning("Encryption key not found in SecureStorage (Key: {Key}).", StorageItems.EncryptionKey);
                return null;
            }
            return Convert.FromHexString(encryptionKeyHex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get and convert EncryptionKey from SecureStorage.");
            return null;
        }
    }
    public async Task<byte[]?> GetPrivateKeyAsByteAsync()
    {
        try
        {
            var privateKeyHex = await _secureStorage.GetAsync(StorageItems.PrivateKey);
            if (string.IsNullOrEmpty(privateKeyHex))
            {
                _logger.LogError("CRITICAL: Private key not found in SecureStorage (Key: {Key}).", StorageItems.PrivateKey);
                return null;
            }
            return Convert.FromHexString(privateKeyHex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get and convert PrivateKey from SecureStorage.");
            return null;
        }
    }
    public DateTime? GetAccessTokenExpiration() => GetPreference<DateTime?>(StorageItems.AccessTokenExpiration, null);
    public DateTime? GetRefreshTokenExpiration() => GetPreference<DateTime?>(StorageItems.RefreshTokenExpiration, null);

    public async Task<string?> GetUniqueDeviceIdAsync() => await GetSecureAsync(StorageItems.UniqueDeviceId);
    public async Task SetTokensAsync(AuthResponseDto authResponse)
    {
        try
        {
            await _secureStorage.SetAsync(StorageItems.AccessToken, authResponse.AccessToken);
            _preferences.Set(StorageItems.AccessTokenExpiration, authResponse.AccessTokenExpiration);

            await _secureStorage.SetAsync(StorageItems.RefreshToken, authResponse.RefreshToken);
            _preferences.Set(StorageItems.RefreshTokenExpiration, authResponse.RefreshTokenExpiration);

            _logger.LogInformation("Access and Refresh tokens saved to storage.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save tokens to storage.");
        }
    }
    public async Task SetKeysAsync(byte[] privateKey, byte[] encryptionKey)
    {
        try
        {
            await _secureStorage.SetAsync(StorageItems.EncryptionKey, Convert.ToHexString(encryptionKey));
            await _secureStorage.SetAsync(StorageItems.PrivateKey, Convert.ToHexString(privateKey));
            _logger.LogInformation("Encryption and Private keys saved to SecureStorage.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save keys to SecureStorage.");
        }
    }


    public async Task SetUniqueDeviceIdAsync(string id)
    {
        try
        {
            await _secureStorage.SetAsync(StorageItems.UniqueDeviceId, id);
            _logger.LogInformation("UniqueDeviceId saved to SecureStorage.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save UniqueDeviceId to SecureStorage.");
        }
    }

    public async Task<byte[]> GetOrCreateDbKeyAsync()
    {
        try
        {
            string? storedKeyBase64 = await _secureStorage.GetAsync(StorageItems.DbEncryptionKey);

            if (!string.IsNullOrEmpty(storedKeyBase64))
            {
                _logger.LogInformation("Found existing Realm DB encryption key.");
                return Convert.FromBase64String(storedKeyBase64);
            }
            else
            {
                _logger.LogInformation("No existing Realm DB encryption key found. Creating a new one...");
                byte[] newKey = RandomNumberGenerator.GetBytes(64);
                await _secureStorage.SetAsync(StorageItems.DbEncryptionKey, Convert.ToBase64String(newKey));
                _logger.LogInformation("New Realm DB encryption key created and saved.");
                return newKey;
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to get or create Realm DB encryption key. Database will not be accessible.");
            throw;
        }
    }

    public async Task<string?> GetOrCreateDbPathAsync()
    {
        try
        {
            var dbPath = await _secureStorage.GetAsync(StorageItems.DbPathKey);
            if (!string.IsNullOrEmpty(dbPath))
            {
                _logger.LogInformation("Found existing Realm DB path: {DbPath}", dbPath);
                return dbPath;
            }
            else
            {
                _logger.LogInformation("No existing Realm DB path found. Creating a new one...");
                string newDbName = $"securevaultdb-{Guid.NewGuid()}.realm";
                var newDbPath = Path.Combine(FileSystem.Current.AppDataDirectory, newDbName);
                await _secureStorage.SetAsync(StorageItems.DbPathKey, newDbPath);
                _logger.LogInformation("New Realm DB path created: {DbPath}", newDbPath);
                return newDbPath;
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to get or create Realm DB path. Database will not be accessible.");
            return null;
        }
    }

    public async Task<string?> GetDPoPKeyAsync() => await GetSecureAsync(StorageItems.DPoPPrivateJwk);

    public async Task SetDPoPKeyAsync(string dpopJwk)
    {
        try
        {
            await _secureStorage.SetAsync(StorageItems.DPoPPrivateJwk, dpopJwk);
            _logger.LogInformation("DPoP private JWK saved to SecureStorage.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save DPoP private JWK to SecureStorage.");
        }
    }

    //Preferences
    public void SetLastSyncDate(DateTimeOffset syncDate) => _preferences.Set(StorageItems.LastSyncDateKey, syncDate);
    public DateTimeOffset GetLastSyncDate() => _preferences.Get(StorageItems.LastSyncDateKey, DateTimeOffset.MinValue);
    public void SetAuthenticationState(bool authState) => _preferences.Set(StorageItems.IsAuthenticated, authState);
    public bool IsAuthenticated() => _preferences.Get(StorageItems.IsAuthenticated, false);
    public void SetEmail(string email) => _preferences.Set(StorageItems.CurrentUserEmail, email);
    public string GetEmail() => GetPreference(StorageItems.CurrentUserEmail, string.Empty);


    // Clear all data except UniqueDeviceId
    public async Task ClearAll()
    {
        try
        {
            _logger.LogInformation("Clearing all user data (except UniqueDeviceId)...");
            var uniqueDeviceId = await GetUniqueDeviceIdAsync();

            _secureStorage.RemoveAll();
            _preferences.Clear();

            if (!string.IsNullOrEmpty(uniqueDeviceId))
                await SetUniqueDeviceIdAsync(uniqueDeviceId);

            _logger.LogInformation("All user data cleared successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear all user data.");
        }
    }

}
