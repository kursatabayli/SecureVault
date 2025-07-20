using SecureVault.App.Services.Service.Contracts;

namespace SecureVault.App.Services.AuthHelpers
{
    public class DeviceHeadersHandler : DelegatingHandler
    {
        private readonly IDeviceInfoService _deviceInfoService;

        public DeviceHeadersHandler(IDeviceInfoService deviceInfoService)
        {
            _deviceInfoService = deviceInfoService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
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
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
