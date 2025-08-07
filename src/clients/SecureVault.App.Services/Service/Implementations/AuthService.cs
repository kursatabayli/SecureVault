using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Refit;
using SecureVault.App.Services.APIs;
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
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly IBouncyCastleCryptoService _bouncyCastleCryptoService;
        private readonly LogoutService _logoutService;
        private readonly ISecureVaultApi _secureVaultApi;
        private readonly IStorageService _storageService;
        public AuthService(IHashService hashService,
                           AuthenticationStateProvider authenticationStateProvider,
                           IBouncyCastleCryptoService bouncyCastleCryptoService,
                           LogoutService logoutService,
                           ISecureVaultApi secureVaultApi,
                           IStorageService storageService)
        {
            _hashService = hashService;
            _authenticationStateProvider = authenticationStateProvider;
            _bouncyCastleCryptoService = bouncyCastleCryptoService;
            _logoutService = logoutService;
            _secureVaultApi = secureVaultApi;
            _storageService = storageService;
        }
        public async Task<Result?> LoginAsync(LoginModel loginModel)
        {
            try
            {
                var challengeModel = await _secureVaultApi.GetChallengeAsync(loginModel.Email);

                var masterSecret = _hashService.CreateMasterSecret(loginModel.Password, challengeModel.Salt);
                byte[] privateKey = _hashService.GetPrivateKeyForAuth(masterSecret, challengeModel.Salt);

                var signatureHex = SignChallenge(challengeModel.Challenge, privateKey);

                var loginCredentials = new LoginCredentialsModel(loginModel.Email, signatureHex);
                var authResponse = await _secureVaultApi.LoginAsync(loginModel.RememberMe, loginCredentials);

                await _storageService.SetTokensAsync(authResponse);
                byte[] encryptionKey = _hashService.GetEncryptionKeyForData(masterSecret, challengeModel.Salt);
                await _storageService.SetKeysAsync(privateKey, encryptionKey);
                ((CustomAuthStateProvider)_authenticationStateProvider).NotifyUserAuthentication(authResponse.AccessToken);

                return Result.Success();
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.NetworkError", "Bir hata oluştu."));
            }
        }

        public async Task<Result?> RegisterAsync(RegisterUserModel registerUserDto)
        {
            try
            {
                var response = await _secureVaultApi.RegisterAsync(registerUserDto);
                if (response.IsSuccessStatusCode)
                    return Result.Success();

                var error = await response.Error.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.RegisterFailed", "Kayıt başarısız."));
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.NetworkError", "Bir hata oluştu."));
            }
        }
        public async Task<Result?> RefreshTokenAsync()
        {
            var refreshToken = await _storageService.GetRefreshTokenAsync();
            var refreshTokenExpiration = await _storageService.GetRefreshTokenExpirationAsync();

            if (string.IsNullOrEmpty(refreshToken) || refreshTokenExpiration <= DateTime.UtcNow)
            {
                await LogoutAsync();
                return Result.Failure(new Error("SessionExpired", "Oturum süresi doldu."));
            }

            try
            {
                var authResponse = await _secureVaultApi.RefreshTokenAsync(refreshToken);
                await _storageService.SetTokensAsync(authResponse);
                return Result.Success();
            }
            catch (ApiException ex)
            {
                await LogoutAsync();
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Auth.InvalidRefreshToken", "Token yenileme başarısız."));
            }
        }

        public async Task<Result?> LogoutAsync()
        {
            var refreshToken = await _storageService.GetRefreshTokenAsync();
            var accessToken = await _storageService.GetAccessTokenAsync();

            if (!string.IsNullOrEmpty(refreshToken) && !string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    await _secureVaultApi.LogoutAsync(refreshToken);
                }
                catch (ApiException)
                {
                }
            }

            _storageService.ClearAll();

            ((CustomAuthStateProvider)_authenticationStateProvider).NotifyUserLogout();
            _logoutService.RequestLogout();
            return Result.Success();
        }


        //Login Helpers
        private string SignChallenge(string challenge, byte[] privateKeyBytes)
        {
            var msgBytes = Encoding.UTF8.GetBytes(challenge);
            var msgHash = SHA256.HashData(msgBytes);

            return _bouncyCastleCryptoService.SignHash(msgHash, privateKeyBytes);
        }
    }
}
