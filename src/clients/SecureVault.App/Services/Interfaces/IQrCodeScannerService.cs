namespace SecureVault.App.Services.Interfaces;

public interface IQrCodeScannerService
{
    Task<string> ScanAsync();
}