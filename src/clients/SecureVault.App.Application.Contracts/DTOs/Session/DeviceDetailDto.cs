namespace SecureVault.App.Application.Contracts.DTOs.Session;

public class DeviceDetailDto
{
    public string UniqueDeviceId { get; init; }
    public string? DeviceName { get; init; }
    public string? DeviceModel { get; init; }
    public string? DeviceManufacturer { get; init; }
    public string? OperatingSystem { get; init; }
}
