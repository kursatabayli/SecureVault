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
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(value!);
        }

        public async Task SetAsync(string key, object data, TimeSpan? expiry = null)
        {
            var jsonValue = JsonSerializer.Serialize(data);
            await _database.StringSetAsync(key, jsonValue, expiry);
        }

        public async Task RemoveAsync(string key)
        {
            await _database.KeyDeleteAsync(key);
        }
    }
}
