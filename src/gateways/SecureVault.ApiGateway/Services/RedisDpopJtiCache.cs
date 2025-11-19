using Microsoft.Extensions.Caching.Distributed;

namespace SecureVault.ApiGateway.Services;

public class RedisDpopJtiCache : IDpopJtiCache
{
  private readonly IDistributedCache _cache;
  private readonly ILogger<RedisDpopJtiCache> _logger;
  private const string CacheKeyPrefix = "dpop_jti:";

  public RedisDpopJtiCache(IDistributedCache cache, ILogger<RedisDpopJtiCache> logger)
  {
    _cache = cache;
    _logger = logger;
  }

  public async Task<bool> IsJtiReplayedAsync(string jti)
  {
    var cacheKey = CacheKeyPrefix + jti;
    try
    {
      var value = await _cache.GetStringAsync(cacheKey);
      bool isReplay = !string.IsNullOrEmpty(value);

      if (isReplay)
        _logger.LogDebug("DPoP JTI REPLAY DETECTED (found in cache): {Jti}", jti);
      else
        _logger.LogDebug("DPoP JTI not found in cache (not a replay): {Jti}", jti);

      return isReplay;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to check DPoP JTI in cache (connection issue?). Assuming not a replay (fail-open). JTI: {Jti}", jti);
      return false;
    }
  }

  public async Task StoreJtiAsync(string jti, DateTimeOffset expiration)
  {
    var cacheKey = CacheKeyPrefix + jti;
    var options = new DistributedCacheEntryOptions
    {
      AbsoluteExpiration = expiration
    };

    try
    {
      await _cache.SetStringAsync(cacheKey, "used", options);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "CRITICAL: Failed to store DPoP JTI in cache for replay prevention! JTI: {Jti}. Replay attacks are possible until cache recovers.", jti);
    }
  }
}