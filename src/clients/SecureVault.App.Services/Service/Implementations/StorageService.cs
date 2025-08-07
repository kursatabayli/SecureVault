using SecureVault.App.Services.Constants;
using SecureVault.App.Services.Models.AuthModels;
using SecureVault.App.Services.Service.Contracts;

namespace SecureVault.App.Services.Service.Implementations
{
    public class StorageService : IStorageService
    {
        public async Task<string?> GetAccessTokenAsync() => await SecureStorage.Default.GetAsync(StorageItems.AccessToken);
        public async Task<string?> GetRefreshTokenAsync() => await SecureStorage.Default.GetAsync(StorageItems.RefreshToken);
        public async Task<string?> GetPrivateKeyAsync() => await SecureStorage.Default.GetAsync(StorageItems.PrivateKey);
        public async Task<string?> GetEncryptionKeyAsync() => await SecureStorage.Default.GetAsync(StorageItems.EncryptionKey);
        public async Task<DateTime?> GetAccessTokenExpirationAsync()
        {
            var expirationString = await SecureStorage.Default.GetAsync(StorageItems.AccessTokenExpiration);
            if (DateTime.TryParse(expirationString, out var expirationDate))
            {
                return expirationDate;
            }
            return null;
        }
        public async Task<DateTime?> GetRefreshTokenExpirationAsync()
        {
            var expirationString = await SecureStorage.Default.GetAsync(StorageItems.RefreshTokenExpiration);
            if (DateTime.TryParse(expirationString, out var expirationDate))
            {
                return expirationDate;
            }
            return null;
        }
        public async Task<string?> GetUniqueDeviceIdAsync() => await SecureStorage.Default.GetAsync(StorageItems.UniqueDeviceId);

        public async Task SetTokensAsync(AuthResponseModel authResponse)
        {
            await SecureStorage.Default.SetAsync(StorageItems.AccessToken, authResponse.AccessToken);
            await SecureStorage.Default.SetAsync(StorageItems.AccessTokenExpiration, authResponse.AccessTokenExpiration.ToString("o"));

            if (!string.IsNullOrEmpty(authResponse.RefreshToken))
            {
                await SecureStorage.Default.SetAsync(StorageItems.RefreshToken, authResponse.RefreshToken);
                if (authResponse.RefreshTokenExpiration.HasValue)
                {
                    await SecureStorage.Default.SetAsync(StorageItems.RefreshTokenExpiration, authResponse.RefreshTokenExpiration.Value.ToString("o"));
                }
            }
        }

        public async Task SetKeysAsync(byte[] privateKey, byte[] encryptionKey)
        {
            await SecureStorage.Default.SetAsync(StorageItems.EncryptionKey, Convert.ToHexString(encryptionKey));
            await SecureStorage.Default.SetAsync(StorageItems.PrivateKey, Convert.ToHexString(privateKey));
        }

        public void ClearAll()
        {
            SecureStorage.Default.Remove(StorageItems.AccessToken);
            SecureStorage.Default.Remove(StorageItems.AccessTokenExpiration);
            SecureStorage.Default.Remove(StorageItems.RefreshToken);
            SecureStorage.Default.Remove(StorageItems.RefreshTokenExpiration);
            SecureStorage.Default.Remove(StorageItems.PrivateKey);
            SecureStorage.Default.Remove(StorageItems.EncryptionKey);
        }

        public async Task SetUniqueDeviceIdAsync(string id) => await SecureStorage.Default.SetAsync(StorageItems.UniqueDeviceId, id);
    }
}
