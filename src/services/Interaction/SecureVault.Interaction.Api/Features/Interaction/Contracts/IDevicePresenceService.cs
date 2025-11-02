namespace SecureVault.Interaction.Api.Features.Interaction.Contracts;

public interface IDevicePresenceService
{
  Task RegisterDeviceAsync(string userId, string deviceId);
  Task UnregisterDeviceAsync(string userId, string deviceId);
  Task<List<string>> GetActiveDevicesAsync(string userId);
}
