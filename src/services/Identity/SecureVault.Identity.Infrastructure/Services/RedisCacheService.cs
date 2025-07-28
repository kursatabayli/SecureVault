using SecureVault.Identity.Application.Contracts.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace SecureVault.Identity.Infrastructure.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty)
                return default;

            if (typeof(T) == typeof(string))
                return (T)(object)value.ToString();

            return JsonSerializer.Deserialize<T>(value!);
        }

        public async Task SetAsync(string key, object data, TimeSpan? expiry = null)
        {
            if (data is string stringValue)
            {
                await _database.StringSetAsync(key, stringValue, expiry);
                return;
            }

            var jsonValue = JsonSerializer.Serialize(data);
            await _database.StringSetAsync(key, jsonValue, expiry);
        }

        public async Task RemoveAsync(string key) => await _database.KeyDeleteAsync(key);

        public async Task<bool> ExistsAsync(string key) => await _database.KeyExistsAsync(key);
    }
}
