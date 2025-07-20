namespace SecureVault.App.Services.Service.Contracts
{
    public interface IDeviceInfoService
    {
        Task<string> GetUniqueDeviceIdAsync();
        string GetDeviceModel();
        string GetDeviceManufacturer();
        string GetDeviceName();
        string GetOperatingSystemInfo();
        //Task<string> GetPublicIpAddressAsync();
    }
}
