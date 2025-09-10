using Refit;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Contracts.DTOs.QrCodeLogin;
using SecureVault.App.Application.Contracts.DTOs.Register;

namespace SecureVault.App.Infrastructure.Services.Api
{
    public interface ISecureVaultAnonymousApi
    {
        //AuthService
        [Headers("X-Anonymous: true")]
        [Post("/identity/api/Register/")]
        Task<IApiResponse> RegisterAsync([Body] RegisterUserDto registerUserDto, CancellationToken cancellationToken);

        [Headers("X-Anonymous: true")]
        [Get("/identity/api/Auth/challenge/{email}")]
        Task<ChallengeDto> GetChallengeAsync(string email, CancellationToken cancellationToken);

        [Headers("X-Anonymous: true")]
        [Post("/identity/api/Auth/login")]
        Task<AuthResponseDto> LoginAsync([Query] bool rememberMe, [Body] LoginCredentialsDto loginCredentials, CancellationToken cancellationToken);

        [Headers("X-Anonymous: true")]
        [Post("/identity/api/Auth/refresh/")]
        Task<AuthResponseDto> RefreshTokenAsync([Header("X-Refresh-Token")] string refreshToken, CancellationToken cancellationToken);

        [Headers("X-Anonymous: true")]
        [Post("/identity/api/Auth/logout/")]
        Task<IApiResponse> LogoutAsync([Header("X-Refresh-Token")] string refreshToken, CancellationToken cancellationToken);


        //InteractionService
        [Headers("X-Anonymous: true")]
        [Post("/interaction/api/qrlogin/create-channel")]
        Task<CreateChannelDto> CreateChannelAsync(CancellationToken cancellationToken);
    }
}
