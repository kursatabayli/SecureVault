using SecureVault.Interaction.Api.Features.Interaction.Contracts;
using StackExchange.Redis;

namespace SecureVault.Interaction.Api.Features.Interaction.Services;

public class RedisDevicePresenceService : IDevicePresenceService
{
  private readonly IDatabase _redisDb;

  public RedisDevicePresenceService(IConnectionMultiplexer redis)
  {
    _redisDb = redis.GetDatabase();
  }

  private string GetUserKey(string userId) => $"presence:user:{userId}";

  public async Task RegisterDeviceAsync(string userId, string deviceId)
  {
    await _redisDb.SetAddAsync(GetUserKey(userId), deviceId);
  }

  public async Task UnregisterDeviceAsync(string userId, string deviceId)
  {
    await _redisDb.SetRemoveAsync(GetUserKey(userId), deviceId);
  }

  public async Task<List<string>> GetActiveDevicesAsync(string userId)
  {
    var devices = await _redisDb.SetMembersAsync(GetUserKey(userId));
    return devices.Select(d => d.ToString()).ToList();
  }
}
