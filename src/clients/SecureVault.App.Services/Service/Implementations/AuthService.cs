using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SecureVault.App.Services.AuthHelpers;
using SecureVault.App.Services.Constants;
using SecureVault.App.Services.Models.AuthModels;
using SecureVault.App.Services.Models.RegisterModels;
using SecureVault.App.Services.Service.Contracts;
using SecureVault.Shared.Result;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace SecureVault.App.Services.Service.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IHashService _hashService;
        private readonly DeviceHeaderService _deviceHeaderService;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly IBouncyCastleCryptoService _bouncyCastleCryptoService;
        private readonly IApiClient _apiClient;
        private readonly LogoutService _logoutService;
        public AuthService(IHashService hashService, AuthenticationStateProvider authenticationStateProvider, DeviceHeaderService deviceHeaderService, IBouncyCastleCryptoService bouncyCastleCryptoService, IApiClient apiClient, LogoutService logoutService)
        {
            _hashService = hashService;
            _authenticationStateProvider = authenticationStateProvider;
            _deviceHeaderService = deviceHeaderService;
            _bouncyCastleCryptoService = bouncyCastleCryptoService;
            _apiClient = apiClient;
            _logoutService = logoutService;
        }

        public async Task<Result?> LoginAsync(LoginModel loginModel)
        {
            var challengeResult = await GetChallengeFromServer(loginModel.Email);
            if (!challengeResult.IsSuccess)
                return Result.Failure(challengeResult.Error);

            var masterSecret = _hashService.CreateMasterSecret(loginModel.Password, challengeResult.Value.Salt);
            byte[] privateKey = _hashService.GetPrivateKeyForAuth(masterSecret, challengeResult.Value.Salt);

            var signatureHex = SignChallenge(challengeResult.Value.Challenge, privateKey);

            var result = await SendLoginRequestAsync(loginModel.Email, signatureHex, loginModel.RememberMe);

            if (!result.IsSuccess)
                return Result.Failure(result.Error);

            await SetTokens(result.Value);
            byte[] encryptionKey = _hashService.GetEncryptionKeyForData(masterSecret, challengeResult.Value.Salt);
            await SetKeys(privateKey, encryptionKey);
            return Result.Success();
        }

        public async Task<Result?> RegisterAsync(RegisterUserModel registerUserDto) => await _apiClient.PostAsync(Endpoints.RegisterBaseUrl, registerUserDto);

        public async Task<Result?> RefreshTokenAsync()
        {
            var refreshToken = await SecureStorage.Default.GetAsync(StorageItems.RefreshToken);
            var refreshTokenExpirationString = await SecureStorage.Default.GetAsync(StorageItems.RefreshTokenExpiration);

            if (string.IsNullOrEmpty(refreshToken) || 
                    string.IsNullOrEmpty(refreshTokenExpirationString) ||
                    !DateTime.TryParse(refreshTokenExpirationString, out var expirationDate) ||
                    expirationDate.ToUniversalTime() <= DateTime.UtcNow)
            {
                await LogoutAsync();
                return Result.Failure(new Error("SessionExpired", "Oturum süresi doldu."));
            }
            var accessToken = await SecureStorage.Default.GetAsync(StorageItems.AccessToken);

            var result = await _apiClient.PostAsync<object, AuthResponseModel>(
                Endpoints.RefreshTokenUrl,
                payload: null,
                configureRequestAsync: async (request) =>
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    request.Headers.TryAddWithoutValidation("X-Refresh-Token", refreshToken);
                    await _deviceHeaderService.AddDeviceHeadersAsync(request);
                }
            );

            if (result.IsFailure)
            {
                await LogoutAsync();
                return Result.Failure(result.Error);
            }

            await SetTokens(result.Value);
            return Result.Success();
        }

        public async Task<Result?> LogoutAsync()
        {
            var refreshToken = await SecureStorage.Default.GetAsync(StorageItems.RefreshToken);
            var accessToken = await SecureStorage.Default.GetAsync(StorageItems.AccessToken);

            if (!string.IsNullOrEmpty(refreshToken) && !string.IsNullOrEmpty(accessToken))
            {
                await _apiClient.PostAsync(
                    Endpoints.LogoutUrl,
                    configureRequestAsync: (request) => 
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        request.Headers.TryAddWithoutValidation("X-Refresh-Token", refreshToken);
                        return Task.CompletedTask;
                    });
            }

            SecureStorage.Default.Remove(StorageItems.AccessToken);
            SecureStorage.Default.Remove(StorageItems.RefreshToken);
            SecureStorage.Default.Remove(StorageItems.PrivateKey);
            SecureStorage.Default.Remove(StorageItems.EncryptionKey);

            ((CustomAuthStateProvider)_authenticationStateProvider).NotifyUserLogout();
            _logoutService.RequestLogout();
            return Result.Success();
        }


        //Login Helpers
        private async Task<Result<ChallengeModel?>> GetChallengeFromServer(string email) => await _apiClient.GetAsync<ChallengeModel>(Endpoints.ChallengeUrl + email);
        private string SignChallenge(string challenge, byte[] privateKeyBytes)
        {
            var msgBytes = Encoding.UTF8.GetBytes(challenge);
            var msgHash = SHA256.HashData(msgBytes);

            return _bouncyCastleCryptoService.SignHash(msgHash, privateKeyBytes);
        }
        private async Task<Result<AuthResponseModel>?> SendLoginRequestAsync(string email, string signatureHex, bool rememberMe)
        {
            var loginDto = new LoginCredentialsModel(email, signatureHex);
            var requestUrl = string.Format(Endpoints.LoginUrl, rememberMe);

            return await _apiClient.PostAsync<LoginCredentialsModel, AuthResponseModel>(
                requestUrl,
                loginDto,
                configureRequestAsync: async (request) => await _deviceHeaderService.AddDeviceHeadersAsync(request)
            );
        }
        private async Task SetTokens(AuthResponseModel authResponseModel)
        {
            await SecureStorage.Default.SetAsync(StorageItems.AccessToken, authResponseModel.AccessToken);
            await SecureStorage.Default.SetAsync(StorageItems.AccessTokenExpiration, authResponseModel.AccessTokenExpiration.ToString("o"));
            if (!string.IsNullOrEmpty(authResponseModel.RefreshToken))
            {
                await SecureStorage.Default.SetAsync(StorageItems.RefreshToken, authResponseModel.RefreshToken);
                await SecureStorage.Default.SetAsync(
                    StorageItems.RefreshTokenExpiration,
                    authResponseModel.RefreshTokenExpiration.HasValue
                        ? authResponseModel.RefreshTokenExpiration.Value.ToString("o")
                        : string.Empty
                );
            }
            ((CustomAuthStateProvider)_authenticationStateProvider).NotifyUserAuthentication(authResponseModel.AccessToken);
        }
        private async Task SetKeys(byte[] privateKey, byte[] encryptionKey)
        {
            await SecureStorage.Default.SetAsync(StorageItems.EncryptionKey, Convert.ToHexString(encryptionKey));
            await SecureStorage.Default.SetAsync(StorageItems.PrivateKey, Convert.ToHexString(privateKey));
        }
    }
}
