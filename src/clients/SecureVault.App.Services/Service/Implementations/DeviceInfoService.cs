using SecureVault.App.Services.Service.Contracts;

namespace SecureVault.App.Services.Service.Implementations
{
    public class DeviceInfoService : IDeviceInfoService
    {
        //private readonly INetworkService _networkService;

        //public DeviceInfoService(INetworkService networkService)
        //{
        //    _networkService = networkService;
        //}

        private string _cachedUniqueId = null;

        public async Task<string> GetUniqueDeviceIdAsync()
        {
            if (_cachedUniqueId != null)
                return _cachedUniqueId;

            var id = await SecureStorage.GetAsync("UniqueId");

            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                await SecureStorage.SetAsync("UniqueId", id);
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
            //var p when p == DevicePlatform.iOS => $"iOS {DeviceInfo.Current.VersionString}",
            //var p when p == DevicePlatform.MacCatalyst => $"macOS {DeviceInfo.Current.VersionString}",

            _ => $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}"
        };
        //public async Task<string> GetPublicIpAddressAsync() => await _networkService.GetPublicIpAddressAsync();
    }
}
