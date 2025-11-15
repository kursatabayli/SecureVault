using Microsoft.Extensions.Caching.Distributed;
using SecureVault.Identity.Application.Contracts.Services;
using System.Text;
using System.Text.Json;

namespace SecureVault.Identity.Infrastructure.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _cache.GetAsync(key);

            if (value == null || value.Length == 0)
                return default;

            if (typeof(T) == typeof(string))
            {
                return (T)(object)Encoding.UTF8.GetString(value);
            }

            return JsonSerializer.Deserialize<T>(value);
        }

        public async Task SetAsync(string key, object data, TimeSpan? expiry = null)
        {
            var options = new DistributedCacheEntryOptions();
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry;
            }

            byte[] bytes;
            if (data is string stringValue)
            {
                bytes = Encoding.UTF8.GetBytes(stringValue);
            }
            else
            {
                bytes = JsonSerializer.SerializeToUtf8Bytes(data);
            }

            await _cache.SetAsync(key, bytes, options);
        }

        public async Task RemoveAsync(string key) => await _cache.RemoveAsync(key);

        public async Task<bool> ExistsAsync(string key)
        {
            var value = await _cache.GetAsync(key);
            return value != null;
        }
    }
}
