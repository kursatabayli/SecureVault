using Microsoft.Extensions.Caching.Distributed;

namespace SecureVault.ApiGateway.Services;

public class RedisDpopJtiCache : IDpopJtiCache
{
  private readonly IDistributedCache _cache;
  private const string CacheKeyPrefix = "dpop_jti:";

  public RedisDpopJtiCache(IDistributedCache cache)
  {
    _cache = cache;
  }

  public async Task<bool> IsJtiReplayedAsync(string jti)
  {
    var cacheKey = CacheKeyPrefix + jti;
    var value = await _cache.GetStringAsync(cacheKey);
    return !string.IsNullOrEmpty(value);
  }

  public async Task StoreJtiAsync(string jti, DateTimeOffset expiration)
  {
    var cacheKey = CacheKeyPrefix + jti;
    var options = new DistributedCacheEntryOptions
    {
      AbsoluteExpiration = expiration
    };
    await _cache.SetStringAsync(cacheKey, "used", options);
  }
}