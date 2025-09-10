namespace SecureVault.App.Services
{
    public interface IQrCodeGenerateService
    {
        string GenerateQrCodeAsBase64(string plainText);
        string GenerateQrCodeAsSvg(string plainText);
        string RenderRoundQrCodeAsBase64(string plainText);
    }
}
