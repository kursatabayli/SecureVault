using SecureVault.App.Services.Models.AuthModels;

namespace SecureVault.App.Services.Service.Infrastructure.Contracts
{
    public interface IStorageService
    {
        Task<string?> GetAccessTokenAsync();
        Task<string?> GetRefreshTokenAsync();
        Task<DateTime?> GetAccessTokenExpirationAsync();
        Task<DateTime?> GetRefreshTokenExpirationAsync();
        Task<string?> GetPrivateKeyAsync();
        Task<string?> GetEncryptionKeyAsync();
        Task<string?> GetUniqueDeviceIdAsync();
        Task SetUniqueDeviceIdAsync(string id);
        Task SetTokensAsync(AuthResponseModel authResponse);
        Task SetKeysAsync(byte[] privateKey, byte[] encryptionKey);
        void ClearAll();
    }
}
