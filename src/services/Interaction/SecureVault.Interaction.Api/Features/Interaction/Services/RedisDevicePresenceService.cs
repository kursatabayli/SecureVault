using SecureVault.Interaction.Api.Features.Interaction.Contracts;
using StackExchange.Redis;

namespace SecureVault.Interaction.Api.Features.Interaction.Services;

public class RedisDevicePresenceService : IDevicePresenceService
{
  private readonly IDatabase _redisDb;
  private readonly ILogger<RedisDevicePresenceService> _logger;

  public RedisDevicePresenceService(IConnectionMultiplexer redis, ILogger<RedisDevicePresenceService> logger)
  {
    _redisDb = redis.GetDatabase();
    _logger = logger;
  }

  private string GetUserKey(string userId) => $"presence:user:{userId}";

  public async Task RegisterDeviceAsync(string userId, string deviceId)
  {
    var key = GetUserKey(userId);
    _logger.LogInformation("RegisterDeviceAsync: Adding device {DeviceId} to key {Key} using SetAddAsync...", deviceId, key);
    try
    {
      await _redisDb.SetAddAsync(key, deviceId);
      _logger.LogInformation("RegisterDeviceAsync: Device {DeviceId} added successfully. Key: {Key}", deviceId, key);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "RegisterDeviceAsync: ERROR adding device {DeviceId}. Key: {Key}", deviceId, key);
      throw;
    }
  }

  public async Task UnregisterDeviceAsync(string userId, string deviceId)
  {
    var key = GetUserKey(userId);
    _logger.LogInformation("UnregisterDeviceAsync: Removing device {DeviceId} from key {Key} using SetRemoveAsync...", deviceId, key);
    try
    {
      await _redisDb.SetRemoveAsync(key, deviceId);
      _logger.LogInformation("UnregisterDeviceAsync: Device {DeviceId} removed successfully. Key: {Key}", deviceId, key);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "UnregisterDeviceAsync: ERROR removing device {DeviceId}. Key: {Key}", deviceId, key);
      throw;
    }
  }

  public async Task<List<string>> GetActiveDevicesAsync(string userId)
  {
    var key = GetUserKey(userId);
    _logger.LogInformation("GetActiveDevicesAsync: Reading devices from key {Key}...", key);
    try
    {
      var devices = await _redisDb.SetMembersAsync(key);
      var deviceList = devices.Select(d => d.ToString()).ToList();
      _logger.LogInformation("GetActiveDevicesAsync: Read {Count} devices from key {Key}.", key, deviceList.Count);
      return deviceList;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "GetActiveDevicesAsync: ERROR reading devices. Key: {Key}", key);
      return [];
    }
  }
}