namespace SecureVault.App.Services.Service.Infrastructure.Contracts
{
    public interface IDeviceInfoService
    {
        Task<string> GetUniqueDeviceIdAsync();
        string GetDeviceModel();
        string GetDeviceManufacturer();
        string GetDeviceName();
        string GetOperatingSystemInfo();
    }
}
