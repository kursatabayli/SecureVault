using SecureVault.App.Application.Contracts.DTOs.User;

namespace SecureVault.App.Application.Contracts.DTOs.Register
{
    public class RegisterUserDto
    {
        public string Email { get; set; }
        public byte[] PublicKey { get; set; }
        public byte[] Salt { get; set; }
        public UserInfoDto UserInfo { get; set; }
        public byte[] RecoveryData { get; set; }
    }
}
