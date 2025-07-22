namespace SecureVault.App.Services
{
    public interface IQrCodeScannerService
    {
        Task<string> ScanAsync();
    }

}
