namespace SecureVault.App.Application.Features.CQRS.Sessions.Results;

public class DeviceDetailResult
{
    public string UniqueDeviceId { get; init; }
    public string? DeviceName { get; init; }
    public string? DeviceModel { get; init; }
    public string? DeviceManufacturer { get; init; }
    public string? OperatingSystem { get; init; }
}
