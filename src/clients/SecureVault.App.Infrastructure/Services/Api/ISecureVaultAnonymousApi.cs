using Refit;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Contracts.DTOs.Register;

namespace SecureVault.App.Infrastructure.Services.Api
{
    public interface ISecureVaultAnonymousApi
    {
        //AuthService
        [Post("/identity/api/Register/")]
        Task<IApiResponse> RegisterAsync([Body] RegisterUserDto registerUserDto, CancellationToken cancellationToken);

        [Get("/identity/api/Auth/challenge/{email}")]
        Task<ChallengeDto> GetChallengeAsync(string email, CancellationToken cancellationToken);

        [Post("/identity/api/Auth/login")]
        Task<AuthResponseDto> LoginAsync([Query] bool rememberMe, [Body] LoginCredentialsDto loginCredentials, CancellationToken cancellationToken);

        [Post("/identity/api/Auth/refresh/")]
        Task<AuthResponseDto> RefreshTokenAsync([Header("X-Refresh-Token")] string refreshToken, CancellationToken cancellationToken);

        [Post("/identity/api/Auth/logout/")]
        Task<IApiResponse> LogoutAsync([Header("X-Refresh-Token")] string refreshToken, CancellationToken cancellationToken);
    }
}
