using SecureVault.App.Services.Models.AuthModels;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services.Service.Infrastructure.Contracts
{
    public interface IAuthService
    {
        Task<Result?> LoginAsync(LoginModel loginModel);
        Task<Result?> LogoutAsync();
        Task<Result?> RefreshTokenAsync();
    }
}
