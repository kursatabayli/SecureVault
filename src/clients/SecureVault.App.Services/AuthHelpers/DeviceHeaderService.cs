using SecureVault.App.Services.Service.Contracts;

namespace SecureVault.App.Services.AuthHelpers
{
    public class DeviceHeaderService
    {
        private readonly IDeviceInfoService _deviceInfoService;

        public DeviceHeaderService(IDeviceInfoService deviceInfoService)
        {
            _deviceInfoService = deviceInfoService;
        }

        public async Task AddDeviceHeadersAsync(HttpRequestMessage request)
        {
            var deviceId = await _deviceInfoService.GetUniqueDeviceIdAsync();
            var deviceModel = _deviceInfoService.GetDeviceModel();
            var deviceName = _deviceInfoService.GetDeviceName();
            var deviceManufacturer = _deviceInfoService.GetDeviceManufacturer();
            var operatingSystem = _deviceInfoService.GetOperatingSystemInfo();
            //var publicIpAddress = await _deviceInfoService.GetPublicIpAddressAsync();

            request.Headers.TryAddWithoutValidation("X-Device-Id", deviceId);
            request.Headers.TryAddWithoutValidation("X-Device-Model", deviceModel);
            request.Headers.TryAddWithoutValidation("X-Device-Name", deviceName);
            request.Headers.TryAddWithoutValidation("X-Device-Manufacturer", deviceManufacturer);
            request.Headers.TryAddWithoutValidation("X-Device-OS", operatingSystem);

            //if (!string.IsNullOrEmpty(publicIpAddress))
            //{
            //    request.Headers.TryAddWithoutValidation("X-Real-IP", publicIpAddress);
            //}
        }
    }
}
