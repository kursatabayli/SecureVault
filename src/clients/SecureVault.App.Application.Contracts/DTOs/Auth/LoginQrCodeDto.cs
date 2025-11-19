namespace SecureVault.App.Application.Contracts.DTOs.Auth;

public class LoginQrCodeDto
{
    public string Email { get; set; }
    public byte[] PrivateKey { get; set; }
    public byte[] EncryptionKey { get; set; }
    public bool RememberMe { get; set; }
}
