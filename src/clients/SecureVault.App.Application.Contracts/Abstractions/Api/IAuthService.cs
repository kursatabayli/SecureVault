using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Contracts.Abstractions.Api
{
    public interface IAuthService
    {
        Task<Result?> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken);
        Task<Result> LoginWithQrCodeAsync(LoginQrCodeDto loginQrCodeDto, CancellationToken cancellationToken);
        Task<Result?> LogoutAsync(CancellationToken cancellationToken);
        Task<Result?> RefreshTokenAsync(CancellationToken cancellationToken);
    }
}
