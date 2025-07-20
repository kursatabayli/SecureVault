namespace SecureVault.Identity.Domain.Entities
{
    public class DeviceDetail
    {
        public string UniqueDeviceId { get; init; }
        public string? DeviceName { get; init; }
        public string? DeviceModel { get; init; }
        public string? DeviceManufacturer { get; init; }
        public string? OperatingSystem { get; init; }
    }
}
