using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SecureVault.Identity.Application.Contracts.Services;
using System.Text;
using System.Text.Json;

namespace SecureVault.Identity.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _cache.GetAsync(key);

            if (value == null || value.Length == 0)
            {
                _logger.LogDebug("Cache miss for key: {CacheKey}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {CacheKey}", key);

            if (typeof(T) == typeof(string))
            {
                return (T)(object)Encoding.UTF8.GetString(value);
            }

            return JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data from cache for key: {CacheKey}", key);
            return default;
        }
    }

    public async Task SetAsync(string key, object data, TimeSpan? expiry = null)
    {
        try
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

            _logger.LogDebug("Cache set for key: {CacheKey}. Expiry: {Expiry}", key, expiry?.ToString() ?? "None");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting data in cache for key: {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            _logger.LogDebug("Cache removed for key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing data from cache for key: {CacheKey}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var value = await _cache.GetAsync(key);
            var exists = value != null;
            _logger.LogDebug("Cache exists check for key: {CacheKey}. Result: {Exists}", key, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence in cache for key: {CacheKey}", key);
            return false;
        }
    }
}
