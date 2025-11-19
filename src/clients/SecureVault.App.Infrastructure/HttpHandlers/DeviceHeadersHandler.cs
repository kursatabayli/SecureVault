using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Device;

namespace SecureVault.App.Infrastructure.HttpHandlers;

public class DeviceHeadersHandler : DelegatingHandler
{
    private readonly IDeviceInfoService _deviceInfoService;
    private readonly ILogger<DeviceHeadersHandler> _logger;

    public DeviceHeadersHandler(IDeviceInfoService deviceInfoService, ILogger<DeviceHeadersHandler> logger)
    {
        _deviceInfoService = deviceInfoService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("DeviceHeadersHandler: Adding device headers to request for {RequestUri}...", request.RequestUri);

            var deviceId = await _deviceInfoService.GetUniqueDeviceIdAsync();
            var deviceModel = _deviceInfoService.GetDeviceModel();
            var deviceName = _deviceInfoService.GetDeviceName();
            var deviceManufacturer = _deviceInfoService.GetDeviceManufacturer();
            var operatingSystem = _deviceInfoService.GetOperatingSystemInfo();

            request.Headers.TryAddWithoutValidation("X-Device-Id", deviceId);
            request.Headers.TryAddWithoutValidation("X-Device-Model", deviceModel);
            request.Headers.TryAddWithoutValidation("X-Device-Name", deviceName);
            request.Headers.TryAddWithoutValidation("X-Device-Manufacturer", deviceManufacturer);
            request.Headers.TryAddWithoutValidation("X-Device-OS", operatingSystem);

            _logger.LogDebug("Device headers added: X-Device-Id={DeviceId}, X-Device-OS={DeviceOS}", deviceId, operatingSystem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeviceHeadersHandler: Failed to add device headers to request. The request will proceed without them.");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
