using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Maui.ApplicationModel.Communication;
using SecureVault.App.Services.AuthHelpers;
using SecureVault.App.Services.Constants;
using SecureVault.App.Services.Models.AuthModels;
using SecureVault.App.Services.Models.RegisterModels;
using SecureVault.App.Services.Service.Contracts;
using SecureVault.Shared.Result;
using System.Diagnostics;
using System.Net.Http.Json;
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
        public AuthService(IHashService hashService, AuthenticationStateProvider authenticationStateProvider, DeviceHeaderService deviceHeaderService, IBouncyCastleCryptoService bouncyCastleCryptoService, IApiClient apiClient)
        {
            _hashService = hashService;
            _authenticationStateProvider = authenticationStateProvider;
            _deviceHeaderService = deviceHeaderService;
            _bouncyCastleCryptoService = bouncyCastleCryptoService;
            _apiClient = apiClient;
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
            var refreshToken = await SecureStorage.Default.GetAsync(StorageKeys.RefreshToken);
            var accessToken = await SecureStorage.Default.GetAsync(StorageKeys.AccessToken);
            if (string.IsNullOrEmpty(refreshToken))
                await LogoutAsync();

            var requestPayload = new { refreshToken, accessToken };

            var result = await _apiClient.PostAsync<object, AuthResponseModel>(
                Endpoints.RefreshTokenUrl,
                requestPayload,
                configureRequestAsync: _deviceHeaderService.AddDeviceHeadersAsync
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
            var refreshToken = await SecureStorage.Default.GetAsync(StorageKeys.RefreshToken);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _apiClient.PostAsync(Endpoints.LogoutUrl, new { refreshToken });
            }

            SecureStorage.Default.Remove(StorageKeys.AccessToken);
            SecureStorage.Default.Remove(StorageKeys.RefreshToken);
            SecureStorage.Default.Remove(StorageKeys.PrivateKey);
            SecureStorage.Default.Remove(StorageKeys.EncryptionKey);

            ((CustomAuthStateProvider)_authenticationStateProvider).NotifyUserLogout();

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
            await SecureStorage.Default.SetAsync(StorageKeys.AccessToken, authResponseModel.AccessToken);
            if (!string.IsNullOrEmpty(authResponseModel.RefreshToken))
            {
                await SecureStorage.Default.SetAsync(StorageKeys.RefreshToken, authResponseModel.RefreshToken);
            }
            ((CustomAuthStateProvider)_authenticationStateProvider).NotifyUserAuthentication(authResponseModel.AccessToken);
        }
        private async Task SetKeys(byte[] privateKey, byte[] encryptionKey)
        {
            await SecureStorage.Default.SetAsync(StorageKeys.EncryptionKey, Convert.ToHexString(encryptionKey));
            await SecureStorage.Default.SetAsync(StorageKeys.PrivateKey, Convert.ToHexString(privateKey));
        }
    }
}
