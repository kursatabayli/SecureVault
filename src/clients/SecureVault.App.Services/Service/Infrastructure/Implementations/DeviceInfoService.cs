using SecureVault.App.Services.Constants;
using SecureVault.App.Services.Service.Infrastructure.Contracts;

namespace SecureVault.App.Services.Service.Infrastructure.Implementations
{
    public class DeviceInfoService : IDeviceInfoService
    {
        private readonly IStorageService _storageService;

        public DeviceInfoService(IStorageService storageService)
        {
            _storageService = storageService;
        }

        private string _cachedUniqueId = null;

        public async Task<string> GetUniqueDeviceIdAsync()
        {
            if (_cachedUniqueId != null)
                return _cachedUniqueId;

            var id = await _storageService.GetUniqueDeviceIdAsync();

            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                await _storageService.SetUniqueDeviceIdAsync(id);
            }

            _cachedUniqueId = id;
            return _cachedUniqueId;
        }

        public string GetDeviceModel() => DeviceInfo.Current.Model;
        public string GetDeviceManufacturer() => DeviceInfo.Current.Manufacturer;
        public string GetDeviceName() => DeviceInfo.Current.Name;
        public string GetOperatingSystemInfo() => DeviceInfo.Current.Platform switch
        {
            var p when p == DevicePlatform.WinUI && DeviceInfo.Current.Version.Build >= 22000 => "Windows 11",
            var p when p == DevicePlatform.WinUI => "Windows 10",

            var p when p == DevicePlatform.Android => $"Android {DeviceInfo.Current.VersionString}",

            _ => $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}"
        };
    }
}
