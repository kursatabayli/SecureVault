using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Infrastructure.Helpers.Constants;

namespace SecureVault.App.Infrastructure.Services.Storage
{
    public class StorageService : IStorageService
    {
        public async Task<string?> GetAccessTokenAsync() => await SecureStorage.Default.GetAsync(StorageItems.AccessToken);
        public async Task<string?> GetRefreshTokenAsync() => await SecureStorage.Default.GetAsync(StorageItems.RefreshToken);
        public async Task<string?> GetPrivateKeyAsync() => await SecureStorage.Default.GetAsync(StorageItems.PrivateKey);
        public async Task<byte[]> GetEncryptionKeyAsByteAsync()
        {
            var encryptionKeyHex = await SecureStorage.Default.GetAsync(StorageItems.EncryptionKey);
            if (string.IsNullOrEmpty(encryptionKeyHex))
                throw new InvalidOperationException("Encryption key not found in SecureStorage.");

            var encryptionKey = Convert.FromHexString(encryptionKeyHex);

            return encryptionKey;
        }
        public async Task<byte[]> GetPrivateKeyAsByteAsync()
        {
            var privateKeyHex = await SecureStorage.Default.GetAsync(StorageItems.PrivateKey);
            if (string.IsNullOrEmpty(privateKeyHex))
                throw new InvalidOperationException("Encryption key not found in SecureStorage.");

            var privateKey = Convert.FromHexString(privateKeyHex);

            return privateKey;
        }
        public DateTime? GetAccessTokenExpiration()
        {
            var expirationDate = Preferences.Default.Get<DateTime>(StorageItems.AccessTokenExpiration, default);
            if (expirationDate == default)
                return null;
            return expirationDate;
        }
        public DateTime? GetRefreshTokenExpiration()
        {
            var expirationDate = Preferences.Default.Get<DateTime>(StorageItems.RefreshTokenExpiration, default);
            if (expirationDate == default)
                return null;
            return expirationDate;
        }

        public async Task<string?> GetUniqueDeviceIdAsync() => await SecureStorage.Default.GetAsync(StorageItems.UniqueDeviceId);

        public async Task SetTokensAsync(AuthResponseDto authResponse)
        {
            await SecureStorage.Default.SetAsync(StorageItems.AccessToken, authResponse.AccessToken);
            Preferences.Default.Set(StorageItems.AccessTokenExpiration, authResponse.AccessTokenExpiration);

            await SecureStorage.Default.SetAsync(StorageItems.RefreshToken, authResponse.RefreshToken);
            Preferences.Default.Set(StorageItems.RefreshTokenExpiration, authResponse.RefreshTokenExpiration);
        }

        public async Task SetKeysAsync(byte[] privateKey, byte[] encryptionKey)
        {
            await SecureStorage.Default.SetAsync(StorageItems.EncryptionKey, Convert.ToHexString(encryptionKey));
            await SecureStorage.Default.SetAsync(StorageItems.PrivateKey, Convert.ToHexString(privateKey));
        }


        public async Task SetUniqueDeviceIdAsync(string id) => await SecureStorage.Default.SetAsync(StorageItems.UniqueDeviceId, id);

        //Preferences
        public void SetLastSyncDate(DateTimeOffset syncDate) => Preferences.Default.Set(StorageItems.LastSyncDateKey, syncDate);
        public DateTimeOffset GetLastSyncDate() => Preferences.Default.Get(StorageItems.LastSyncDateKey, DateTimeOffset.MinValue);
        public void SetAuthenticationState(bool authState) => Preferences.Default.Set(StorageItems.IsAuthenticated, authState);
        public bool IsAuthenticated() => Preferences.Default.Get(StorageItems.IsAuthenticated, false);

        public async Task ClearAll()
        {
            var uniqueDeviceId = await GetUniqueDeviceIdAsync();
            SecureStorage.Default.RemoveAll();
            Preferences.Default.Clear();
            if (!string.IsNullOrEmpty(uniqueDeviceId))
                await SetUniqueDeviceIdAsync(uniqueDeviceId);
        }

        public void SetEmail(string email) => Preferences.Default.Set(StorageItems.CurrentUserEmail, email);
        public string GetEmail() => Preferences.Default.Get(StorageItems.CurrentUserEmail, string.Empty);
    }
}
