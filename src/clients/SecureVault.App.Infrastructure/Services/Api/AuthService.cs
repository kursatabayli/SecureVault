using Polly.CircuitBreaker;
using Refit;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.UI;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.Shared.Result;
using System.Security.Cryptography;
using System.Text;

namespace SecureVault.App.Infrastructure.Services.Api
{
    public class AuthService : IAuthService
    {
        private readonly IHashService _hashService;
        private readonly IAuthenticationStateNotifier _authenticationStateNotifier;
        private readonly IBouncyCastleCryptoService _bouncyCastleCryptoService;
        private readonly ISecureVaultAnonymousApi _secureVaultAnonymousApi;
        private readonly IStorageService _storageService;
        public AuthService(IHashService hashService,
                               IAuthenticationStateNotifier authenticationStateNotifier,
                               IBouncyCastleCryptoService bouncyCastleCryptoService,
                               ISecureVaultAnonymousApi secureVaultAnonymousApi,
                               IStorageService storageService)
        {
            _hashService = hashService;
            _authenticationStateNotifier = authenticationStateNotifier;
            _bouncyCastleCryptoService = bouncyCastleCryptoService;
            _secureVaultAnonymousApi = secureVaultAnonymousApi;
            _storageService = storageService;
        }
        public async Task<Result?> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken)
        {
            try
            {
                var challengeDto = await _secureVaultAnonymousApi.GetChallengeAsync(loginDto.Email, cancellationToken);
                var masterSecret = _hashService.CreateMasterSecret(loginDto.Password, challengeDto.Salt);
                byte[] privateKey = _hashService.GetPrivateKeyForAuth(masterSecret, challengeDto.Salt);
                var signatureHex = SignChallenge(challengeDto.Challenge, privateKey);
                var loginCredentials = new LoginCredentialsDto(loginDto.Email, signatureHex);
                var authResponse = await _secureVaultAnonymousApi.LoginAsync(loginDto.RememberMe, loginCredentials, cancellationToken);
                await _storageService.SetTokensAsync(authResponse);
                byte[] encryptionKey = _hashService.GetEncryptionKeyForData(masterSecret, challengeDto.Salt);
                await _storageService.SetKeysAsync(privateKey, encryptionKey);
                _storageService.SetEmail(loginDto.Email);
                await _authenticationStateNotifier.NotifyUserAuthenticated(authResponse.AccessToken);
                return Result.Success();
            }
            catch (BrokenCircuitException)
            {
                return Result.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor. Lütfen birkaç dakika sonra tekrar deneyin."));
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.LoginFailed", "Giriş başarısız."));
            }
            catch (HttpRequestException)
            {
                return Result.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol edin veya daha sonra tekrar deneyin."));
            }
            catch (Exception)
            {
                return Result.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu. Lütfen daha sonra tekrar deneyin."));
            }
        }

        public async Task<Result> LoginWithQrCodeAsync(LoginQrCodeDto loginQrCodeDto, CancellationToken cancellationToken)
        {
            try
            {
                var challengeDto = await _secureVaultAnonymousApi.GetChallengeAsync(loginQrCodeDto.Email, cancellationToken);
                var signatureHex = SignChallenge(challengeDto.Challenge, loginQrCodeDto.PrivateKey);
                var loginCredentials = new LoginCredentialsDto(loginQrCodeDto.Email, signatureHex);
                var authResponse = await _secureVaultAnonymousApi.LoginAsync(loginQrCodeDto.RememberMe, loginCredentials, cancellationToken);

                await _storageService.SetTokensAsync(authResponse);
                await _storageService.SetKeysAsync(loginQrCodeDto.PrivateKey, loginQrCodeDto.EncryptionKey);
                _storageService.SetEmail(loginQrCodeDto.Email);
                await _authenticationStateNotifier.NotifyUserAuthenticated(authResponse.AccessToken);
                return Result.Success();
            }
            catch (BrokenCircuitException)
            {
                return Result.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor. Lütfen birkaç dakika sonra tekrar deneyin."));
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.LoginFailed", "Giriş başarısız."));
            }
            catch (HttpRequestException)
            {
                return Result.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol edin veya daha sonra tekrar deneyin."));
            }
            catch (Exception)
            {
                return Result.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu. Lütfen daha sonra tekrar deneyin."));
            }
        }
        public async Task<Result?> RefreshTokenAsync(CancellationToken cancellationToken)
        {
            var refreshToken = await _storageService.GetRefreshTokenAsync();
            var refreshTokenExpiration = _storageService.GetRefreshTokenExpiration();

            if (string.IsNullOrEmpty(refreshToken) || refreshTokenExpiration <= DateTime.UtcNow)
            {
                await LogoutAsync(cancellationToken);
                return Result.Failure(new Error("SessionExpired", "Oturum süresi doldu."));
            }

            try
            {
                var authResponse = await _secureVaultAnonymousApi.RefreshTokenAsync(refreshToken, cancellationToken);
                await _storageService.SetTokensAsync(authResponse);
                return Result.Success();
            }
            catch (BrokenCircuitException)
            {
                return Result.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor. Lütfen birkaç dakika sonra tekrar deneyin."));
            }
            catch (ApiException ex)
            {
                await LogoutAsync(cancellationToken);
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Auth.InvalidRefreshToken", "Token yenileme başarısız."));
            }
            catch (HttpRequestException)
            {
                return Result.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı. Token yenilenemedi."));
            }
            catch (Exception)
            {
                return Result.Failure(new Error("Client.UnexpectedError", "Token yenileme sırasında beklenmedik bir hata oluştu."));
            }
        }

        public async Task<Result?> LogoutAsync(CancellationToken cancellationToken)
        {
            var refreshToken = await _storageService.GetRefreshTokenAsync();
            var accessToken = await _storageService.GetAccessTokenAsync();

            if (!string.IsNullOrEmpty(refreshToken) && !string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    await _secureVaultAnonymousApi.LogoutAsync(refreshToken, cancellationToken);
                }
                catch (Exception)
                {
                    // Logout sırasında sunucuya ulaşılamasa bile sorun değil.
                    // Akışın devam edip local token'ları temizlemesi daha önemli.
                    // Bu yüzden bu blokta hatayı yutuyoruz.
                }
            }

            await _authenticationStateNotifier.NotifyUserLogout();
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
