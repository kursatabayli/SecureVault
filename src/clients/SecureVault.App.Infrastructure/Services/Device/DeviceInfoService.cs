using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Device;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;

namespace SecureVault.App.Infrastructure.Services.Device;

public class DeviceInfoService : IDeviceInfoService
{
    private readonly IStorageService _storageService;
    private readonly IDeviceInfo _deviceInfo;
    private readonly ILogger<DeviceInfoService> _logger;

    private readonly SemaphoreSlim _lock = new(1, 1);

    public DeviceInfoService(IStorageService storageService, IDeviceInfo deviceInfo, ILogger<DeviceInfoService> logger)
    {
        _storageService = storageService;
        _deviceInfo = deviceInfo;
        _logger = logger;
    }

    private string _cachedUniqueId = null;

    public async Task<string> GetUniqueDeviceIdAsync()
    {
        if (_cachedUniqueId != null)
        {
            _logger.LogTrace("Retrieved UniqueDeviceId from in-memory cache.");
            return _cachedUniqueId;
        }
        await _lock.WaitAsync();
        try
        {
            if (_cachedUniqueId != null)
            {
                _logger.LogTrace("Retrieved UniqueDeviceId from cache (after acquiring lock).");
                return _cachedUniqueId;
            }

            string id;
            try
            {
                id = await _storageService.GetUniqueDeviceIdAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read UniqueDeviceId from SecureStorage. Will attempt to create a new one.");
                id = null;
            }

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogInformation("No UniqueDeviceId found in storage. Creating a new one...");
                id = Guid.NewGuid().ToString();
                try
                {
                    await _storageService.SetUniqueDeviceIdAsync(id);
                    _logger.LogInformation("New UniqueDeviceId saved to SecureStorage.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save new UniqueDeviceId to SecureStorage. The ID will be session-only.");
                }
            }
            else
            {
                _logger.LogInformation("Retrieved existing UniqueDeviceId from SecureStorage.");
            }

            _cachedUniqueId = id;
            return _cachedUniqueId;
        }
        finally
        {
            _lock.Release();
        }
    }

    public string GetDeviceModel()
    {
        var model = _deviceInfo.Model;
        _logger.LogDebug("GetDeviceModel: {Model}", model);
        return model;
    }

    public string GetDeviceManufacturer()
    {
        var manufacturer = _deviceInfo.Manufacturer;
        _logger.LogDebug("GetDeviceManufacturer: {Manufacturer}", manufacturer);
        return manufacturer;
    }

    public string GetDeviceName()
    {
        var name = _deviceInfo.Name;
        _logger.LogDebug("GetDeviceName: {Name}", name);
        return name;
    }

    public string GetOperatingSystemInfo()
    {
        var osInfo = _deviceInfo.Platform switch
        {
            var p when p == DevicePlatform.WinUI && _deviceInfo.Version.Build >= 22000 => "Windows 11",
            var p when p == DevicePlatform.WinUI => "Windows 10",
            var p when p == DevicePlatform.Android => $"Android {_deviceInfo.VersionString}",
            _ => $"{_deviceInfo.Platform} {_deviceInfo.VersionString}"
        };

        _logger.LogDebug("GetOperatingSystemInfo: {OsInfo}", osInfo);
        return osInfo;
    }
}
