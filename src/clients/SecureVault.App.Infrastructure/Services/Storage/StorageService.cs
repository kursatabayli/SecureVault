using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Infrastructure.Helpers.Constants;
using System.Security.Cryptography;

namespace SecureVault.App.Infrastructure.Services.Storage
{
    public class StorageService : IStorageService
    {
        private readonly IPreferences _preferences;
        private readonly ISecureStorage _secureStorage;

        public StorageService(IPreferences preferences, ISecureStorage secureStorage)
        {
            _preferences = preferences;
            _secureStorage = secureStorage;
        }

        public async Task<string?> GetAccessTokenAsync() => await _secureStorage.GetAsync(StorageItems.AccessToken);
        public async Task<string?> GetRefreshTokenAsync() => await _secureStorage.GetAsync(StorageItems.RefreshToken);
        public async Task<string?> GetPrivateKeyAsync() => await _secureStorage.GetAsync(StorageItems.PrivateKey);
        public async Task<byte[]> GetEncryptionKeyAsByteAsync()
        {
            var encryptionKeyHex = await _secureStorage.GetAsync(StorageItems.EncryptionKey);
            if (string.IsNullOrEmpty(encryptionKeyHex))
                throw new InvalidOperationException("Encryption key not found in SecureStorage.");

            var encryptionKey = Convert.FromHexString(encryptionKeyHex);

            return encryptionKey;
        }
        public async Task<byte[]> GetPrivateKeyAsByteAsync()
        {
            var privateKeyHex = await _secureStorage.GetAsync(StorageItems.PrivateKey);
            if (string.IsNullOrEmpty(privateKeyHex))
                throw new InvalidOperationException("Private key not found in SecureStorage.");

            var privateKey = Convert.FromHexString(privateKeyHex);

            return privateKey;
        }
        public DateTime? GetAccessTokenExpiration()
        {
            var expirationDate = _preferences.Get<DateTime>(StorageItems.AccessTokenExpiration, default);
            if (expirationDate == default)
                return null;
            return expirationDate;
        }
        public DateTime? GetRefreshTokenExpiration()
        {
            var expirationDate = _preferences.Get<DateTime>(StorageItems.RefreshTokenExpiration, default);
            if (expirationDate == default)
                return null;
            return expirationDate;
        }

        public async Task<string?> GetUniqueDeviceIdAsync() => await _secureStorage.GetAsync(StorageItems.UniqueDeviceId);

        public async Task SetTokensAsync(AuthResponseDto authResponse)
        {
            await _secureStorage.SetAsync(StorageItems.AccessToken, authResponse.AccessToken);
            _preferences.Set(StorageItems.AccessTokenExpiration, authResponse.AccessTokenExpiration);

            await _secureStorage.SetAsync(StorageItems.RefreshToken, authResponse.RefreshToken);
            _preferences.Set(StorageItems.RefreshTokenExpiration, authResponse.RefreshTokenExpiration);
        }

        public async Task SetKeysAsync(byte[] privateKey, byte[] encryptionKey)
        {
            await _secureStorage.SetAsync(StorageItems.EncryptionKey, Convert.ToHexString(encryptionKey));
            await _secureStorage.SetAsync(StorageItems.PrivateKey, Convert.ToHexString(privateKey));
        }


        public async Task SetUniqueDeviceIdAsync(string id) => await _secureStorage.SetAsync(StorageItems.UniqueDeviceId, id);


        public async Task<byte[]> GetOrCreateDbKeyAsync()
        {
            try
            {
                string? storedKeyBase64 = await _secureStorage.GetAsync(StorageItems.DbEncryptionKey);

                if (!string.IsNullOrEmpty(storedKeyBase64))
                {
                    return Convert.FromBase64String(storedKeyBase64);
                }
                else
                {
                    byte[] newKey = RandomNumberGenerator.GetBytes(64);

                    await _secureStorage.SetAsync(StorageItems.DbEncryptionKey, Convert.ToBase64String(newKey));

                    return newKey;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get or create encryption key: {ex.Message}");
                throw;
            }
        }

        public async Task<string?> GetOrCreateDbPathAsync()
        {
            var dbPath = await _secureStorage.GetAsync(StorageItems.DbPathKey);
            if (!string.IsNullOrEmpty(dbPath))
            {
                return dbPath;
            }
            else
            {
                string newDbName = $"securevaultdb-{Guid.NewGuid()}.realm";
                var newDbPath = Path.Combine(FileSystem.Current.AppDataDirectory, newDbName);
                await _secureStorage.SetAsync(StorageItems.DbPathKey, newDbPath);
                return newDbPath;
            }
        }

        //Preferences
        public void SetLastSyncDate(DateTimeOffset syncDate) => _preferences.Set(StorageItems.LastSyncDateKey, syncDate);
        public DateTimeOffset GetLastSyncDate() => _preferences.Get(StorageItems.LastSyncDateKey, DateTimeOffset.MinValue);
        public void SetAuthenticationState(bool authState) => _preferences.Set(StorageItems.IsAuthenticated, authState);
        public bool IsAuthenticated() => _preferences.Get(StorageItems.IsAuthenticated, false);

        public async Task ClearAll()
        {
            var uniqueDeviceId = await GetUniqueDeviceIdAsync();
            _secureStorage.RemoveAll();
            _preferences.Clear();
            if (!string.IsNullOrEmpty(uniqueDeviceId))
                await SetUniqueDeviceIdAsync(uniqueDeviceId);
        }

        public void SetEmail(string email) => _preferences.Set(StorageItems.CurrentUserEmail, email);
        public string GetEmail() => _preferences.Get(StorageItems.CurrentUserEmail, string.Empty);
    }
}
