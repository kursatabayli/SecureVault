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
    _logger.LogInformation("RegisterDeviceAsync: Cihaz {DeviceId} 'SetAddAsync' ile {Key} anahtarına ekleniyor...", deviceId, key);
    try
    {
      await _redisDb.SetAddAsync(key, deviceId);
      _logger.LogInformation("RegisterDeviceAsync: Cihaz {DeviceId} başarıyla eklendi. Anahtar: {Key}", deviceId, key);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "RegisterDeviceAsync: Cihaz {DeviceId} eklenirken HATA oluştu. Anahtar: {Key}", deviceId, key);
      throw;
    }
  }

  public async Task UnregisterDeviceAsync(string userId, string deviceId)
  {
    var key = GetUserKey(userId);
    _logger.LogInformation("UnregisterDeviceAsync: Cihaz {DeviceId} 'SetRemoveAsync' ile {Key} anahtarından kaldırılıyor...", deviceId, key);
    try
    {
      await _redisDb.SetRemoveAsync(key, deviceId);
      _logger.LogInformation("UnregisterDeviceAsync: Cihaz {DeviceId} başarıyla kaldırıldı. Anahtar: {Key}", deviceId, key);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "UnregisterDeviceAsync: Cihaz {DeviceId} kaldırılırken HATA oluştu. Anahtar: {Key}", deviceId, key);
      throw;
    }
  }

  public async Task<List<string>> GetActiveDevicesAsync(string userId)
  {
    var key = GetUserKey(userId);
    _logger.LogInformation("GetActiveDevicesAsync: {Key} anahtarındaki cihazlar okunuyor...", key);
    try
    {
      var devices = await _redisDb.SetMembersAsync(key);
      var deviceList = devices.Select(d => d.ToString()).ToList();
      _logger.LogInformation("GetActiveDevicesAsync: {Key} anahtarından {Count} adet cihaz okundu.", key, deviceList.Count);
      return deviceList;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "GetActiveDevicesAsync: Cihazlar okunurken HATA oluştu. Anahtar: {Key}", key);
      return new List<string>();
    }
  }
}