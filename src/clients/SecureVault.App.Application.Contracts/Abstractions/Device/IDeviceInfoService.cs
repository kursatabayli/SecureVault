namespace SecureVault.App.Application.Contracts.Abstractions.Device;

public interface IDeviceInfoService
{
    Task<string> GetUniqueDeviceIdAsync();
    string GetDeviceModel();
    string GetDeviceManufacturer();
    string GetDeviceName();
    string GetOperatingSystemInfo();
}
