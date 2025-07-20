using SecureVault.App.Services.Models.UserModels;

namespace SecureVault.App.Services.Models.RegisterModels
{
    public class RegisterUserModel
    {
        public string Email { get; set; }
        public byte[] PublicKey { get; set; }
        public byte[] Salt { get; set; }
        public UserInfoModel UserInfo { get; set; }
    }
}
