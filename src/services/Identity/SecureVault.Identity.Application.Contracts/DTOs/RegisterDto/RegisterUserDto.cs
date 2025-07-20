using SecureVault.Identity.Application.Contracts.DTOs.UserDto;

namespace SecureVault.Identity.Application.Contracts.DTOs.RegisterDto
{
    public class RegisterUserDto
    {
        public string Email { get; set; }
        public byte[] PublicKey { get; set; }
        public byte[] Salt { get; set; }
        public UserInfoDto UserInfo { get; set; }
    }
}
