namespace SecureVault.Identity.Application.Contracts.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync(string key, object data, TimeSpan? expiry = null);
        Task RemoveAsync(string key);
    }
}
