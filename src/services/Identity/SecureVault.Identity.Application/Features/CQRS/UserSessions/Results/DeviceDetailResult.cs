namespace SecureVault.Identity.Application.Features.CQRS.UserSessions.Results
{
    public class DeviceDetailResult
    {
        public string UniqueDeviceId { get; init; }
        public string? DeviceName { get; init; }
        public string? DeviceModel { get; init; }
        public string? DeviceManufacturer { get; init; }
        public string? OperatingSystem { get; init; }
    }
}
