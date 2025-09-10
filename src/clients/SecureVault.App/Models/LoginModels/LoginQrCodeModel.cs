namespace SecureVault.App.Models.LoginModels
{
    public class LoginQrCodeModel
    {
        public string Email { get; set; }
        public byte[] PrivateKey { get; set; }
        public byte[] EncryptionKey { get; set; }
    }
}
