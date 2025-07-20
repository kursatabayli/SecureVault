using Microsoft.AspNetCore.Components.Authorization;
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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHashService _hashService;
        private readonly DeviceHeaderService _deviceHeaderService;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly IBouncyCastleCryptoService _bouncyCastleCryptoService;
        public AuthService(IHashService hashService, IHttpClientFactory httpClientFactory, AuthenticationStateProvider authenticationStateProvider, DeviceHeaderService deviceHeaderService, IBouncyCastleCryptoService bouncyCastleCryptoService)
        {
            _hashService = hashService;
            _httpClientFactory = httpClientFactory;
            _authenticationStateProvider = authenticationStateProvider;
            _deviceHeaderService = deviceHeaderService;
            _bouncyCastleCryptoService = bouncyCastleCryptoService;
        }
        private HttpClient CreateClient(ClientTypes clientTypes = ClientTypes.PublicClient) => _httpClientFactory.CreateClient(clientTypes.ToString());

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

        public async Task<Result?> RegisterAsync(RegisterUserModel registerUserDto)
        {
            try
            {
                var client = CreateClient();
                var response = await client.PostAsJsonAsync(Endpoints.RegisterBaseUrl, registerUserDto);
                if (response.IsSuccessStatusCode)
                    return Result.Success();
                else
                    return await response.Content.ReadFromJsonAsync<Result?>();
            }
            catch (Exception ex)
            {
                Error error = new("RegisterError", ex.Message);
                return Result.Failure(error);
            }
        }

        public async Task<Result?> RefreshTokenAsync()
        {
            var refreshToken = await SecureStorage.Default.GetAsync("RefreshToken");
            var accessToken = await SecureStorage.Default.GetAsync("AccessToken");

            if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(accessToken))
                return Result.Failure(new Error("NoTokens", "Refresh veya Access token bulunamadı."));

            try
            {
                var client = CreateClient();
                var requestPayload = new { refreshToken, accessToken };

                var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.RefreshTokenUrl)
                {
                    Content = JsonContent.Create(requestPayload)
                };

                await _deviceHeaderService.AddDeviceHeadersAsync(request);

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<AuthResponseModel>();
                    if (loginResponse.AccessToken != null && loginResponse.RefreshToken != null)
                    {
                        await SecureStorage.Default.SetAsync("AccessToken", loginResponse.AccessToken);
                        await SecureStorage.Default.SetAsync("RefreshToken", loginResponse.RefreshToken);
                        ((CustomAuthStateProvider)_authenticationStateProvider).NotifyUserAuthentication(loginResponse.AccessToken);
                        return Result.Success();
                    }
                }
                await LogoutAsync();
                return Result.Failure(new Error("RefreshFailed", "Token refresh failed."));
            }
            catch (Exception)
            {
                await LogoutAsync();
                Error error = new("RefreshTokenError", "Token yenileme işlemi sırasında bir hata oluştu.");
                return Result.Failure(error);
            }
        }

        public async Task<Result?> LogoutAsync()
        {
            var refreshToken = await SecureStorage.Default.GetAsync("RefreshToken");
            if (string.IsNullOrEmpty(refreshToken))
                return Result.Failure(new Error("NoRefreshToken", "Refresh token not found."));

            try
            {
                var client = CreateClient();
                await client.PostAsJsonAsync(Endpoints.LogoutUrl, new { refreshToken });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error notifying server of logout: {ex.Message}");
            }
            finally
            {
                SecureStorage.Default.Remove("AccessToken");
                SecureStorage.Default.Remove("RefreshToken");
                ((CustomAuthStateProvider)_authenticationStateProvider).NotifyUserLogout();
            }
            return Result.Success();
        }


        //Login Helpers
        private async Task<Result<ChallengeModel?>> GetChallengeFromServer(string email)
        {
            try
            {
                var client = CreateClient();
                var response = await client.GetAsync(Endpoints.ChallengeUrl + email);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ChallengeModel>();
                }
                else
                {
                    return await response.Content.ReadFromJsonAsync<Error>();
                }
            }
            catch (Exception ex)
            {
                Error error = new("ChallengeRequestFailed", $"An error occurred: {ex.Message}");
                return error;
            }
        }
        private string SignChallenge(string challenge, byte[] privateKeyBytes)
        {
            var msgBytes = Encoding.UTF8.GetBytes(challenge);
            var msgHash = SHA256.HashData(msgBytes);

            return _bouncyCastleCryptoService.SignHash(msgHash, privateKeyBytes);
        }
        private async Task<Result<AuthResponseModel>?> SendLoginRequestAsync(string email, string signatureHex, bool rememberMe)
        {
            try
            {
                var client = CreateClient();
                var loginDto = new LoginCredentialsModel(email, signatureHex);
                var requestUrl = string.Format(Endpoints.LoginUrl, rememberMe);
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = JsonContent.Create(loginDto)
                };

                await _deviceHeaderService.AddDeviceHeadersAsync(request);

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<AuthResponseModel>();
                else
                    return await response.Content.ReadFromJsonAsync<Error>();
            }
            catch (Exception ex)
            {
                Error error = new("LoginRequestFailed", $"An unexpected error occurred: {ex.Message}");
                return error;
            }
        }
        private async Task SetTokens(AuthResponseModel authResponseModel)
        {
            await SecureStorage.Default.SetAsync("AccessToken", authResponseModel.AccessToken);
            if (!string.IsNullOrEmpty(authResponseModel.RefreshToken))
            {
                await SecureStorage.Default.SetAsync("RefreshToken", authResponseModel.RefreshToken);
            }
            ((CustomAuthStateProvider)_authenticationStateProvider).NotifyUserAuthentication(authResponseModel.AccessToken);
        }
        private async Task SetKeys(byte[] privateKey, byte[] encryptionKey)
        {
            await SecureStorage.Default.SetAsync("EncryptionKey", Convert.ToHexString(encryptionKey));
            await SecureStorage.Default.SetAsync("PrivateKey", Convert.ToHexString(privateKey));
        }
    }
}
