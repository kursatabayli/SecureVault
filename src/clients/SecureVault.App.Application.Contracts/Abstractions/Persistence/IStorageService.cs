using SecureVault.App.Application.Contracts.DTOs.Auth;

namespace SecureVault.App.Application.Contracts.Abstractions.Persistence
{
    public interface IStorageService
    {
        Task<string?> GetAccessTokenAsync();
        Task<string?> GetRefreshTokenAsync();
        DateTime? GetAccessTokenExpiration();
        DateTime? GetRefreshTokenExpiration();
        Task<string?> GetPrivateKeyAsync();
        Task<byte[]> GetEncryptionKeyAsByteAsync();
        Task<byte[]> GetPrivateKeyAsByteAsync();
        Task<string?> GetUniqueDeviceIdAsync();
        Task SetUniqueDeviceIdAsync(string id);
        Task SetTokensAsync(AuthResponseDto authResponseDto);
        Task SetKeysAsync(byte[] privateKey, byte[] encryptionKey);

        //Preferences
        void SetEmail(string email);
        string GetEmail();
        void SetLastSyncDate(DateTimeOffset syncDate);
        DateTimeOffset GetLastSyncDate();
        void SetAuthenticationState(bool authState);
        bool IsAuthenticated();
        Task ClearAll();
    }
}
