using SecureVault.App.Services.Models.AuthModels;
using SecureVault.App.Services.Models.RegisterModels;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services.Service.Contracts
{
    public interface IAuthService
    {
        Task<Result?> LoginAsync(LoginModel loginModel);
        Task<Result?> RegisterAsync(RegisterUserModel registerUserDto);
        Task<Result?> LogoutAsync();
        Task<Result?> RefreshTokenAsync();
    }
}
