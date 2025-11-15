using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Refit;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.Shared.Result;
using System.Security.Cryptography;
using System.Text;

namespace SecureVault.App.Infrastructure.Services.Api
{
    public class AuthService : IAuthService
    {
        private readonly IHashService _hashService;
        private readonly IBouncyCastleCryptoService _bouncyCastleCryptoService;
        private readonly ISecureVaultAnonymousApi _secureVaultAnonymousApi;
        private readonly IStorageService _storageService;
        private readonly ILogger<AuthService> _logger;
        public AuthService(IHashService hashService,
                               IBouncyCastleCryptoService bouncyCastleCryptoService,
                               ISecureVaultAnonymousApi secureVaultAnonymousApi,
                               IStorageService storageService,
                               ILogger<AuthService> logger)
        {
            _hashService = hashService;
            _bouncyCastleCryptoService = bouncyCastleCryptoService;
            _secureVaultAnonymousApi = secureVaultAnonymousApi;
            _storageService = storageService;
            _logger = logger;
        }
        public async Task<Result<AuthResponseDto>?> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken)
        {
            try
            {
                var challengeDto = await _secureVaultAnonymousApi.GetChallengeAsync(loginDto.Email, cancellationToken);
                var (masterSecret, privateKey, signatureHex, encryptionKey) = await Task.Run(() =>
                        {
                            var ms = _hashService.CreateMasterSecret(loginDto.Password, challengeDto.Salt);
                            var pk = _hashService.GetPrivateKeyForAuth(ms, challengeDto.Salt);
                            var sig = SignChallenge(challengeDto.Challenge, pk);
                            var ek = _hashService.GetEncryptionKeyForData(ms, challengeDto.Salt);

                            return (ms, pk, sig, ek);
                        }, cancellationToken);

                var loginCredentials = new LoginCredentialsDto(loginDto.Email, signatureHex);
                var authResponse = await _secureVaultAnonymousApi.LoginAsync(loginDto.RememberMe, loginCredentials, cancellationToken);
                await _storageService.SetTokensAsync(authResponse);
                await _storageService.SetKeysAsync(privateKey, encryptionKey);
                _storageService.SetEmail(loginDto.Email);
                return authResponse;
            }
            catch (BrokenCircuitException)
            {
                return Result<AuthResponseDto>.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor. Lütfen birkaç dakika sonra tekrar deneyin."));
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result<AuthResponseDto>.Failure(error ?? new Error("Client.LoginFailed", "Giriş başarısız."));
            }
            catch (HttpRequestException)
            {
                return Result<AuthResponseDto>.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol edin veya daha sonra tekrar deneyin."));
            }
            catch (Exception)
            {
                return Result<AuthResponseDto>.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu. Lütfen daha sonra tekrar deneyin."));
            }
        }

        public async Task<Result<AuthResponseDto>> LoginWithQrCodeAsync(LoginQrCodeDto loginQrCodeDto, CancellationToken cancellationToken)
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
                return authResponse;
            }
            catch (BrokenCircuitException)
            {
                return Result<AuthResponseDto>.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor. Lütfen birkaç dakika sonra tekrar deneyin."));
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result<AuthResponseDto>.Failure(error ?? new Error("Client.LoginFailed", "Giriş başarısız."));
            }
            catch (HttpRequestException)
            {
                return Result<AuthResponseDto>.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol edin veya daha sonra tekrar deneyin."));
            }
            catch (Exception)
            {
                return Result<AuthResponseDto>.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu. Lütfen daha sonra tekrar deneyin."));
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

        public async Task LogoutAsync(CancellationToken cancellationToken)
        {
            var refreshToken = await _storageService.GetRefreshTokenAsync();
            var accessToken = await _storageService.GetAccessTokenAsync();

            if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(accessToken))
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await _secureVaultAnonymousApi.LogoutAsync(refreshToken, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Arka planda çalışan remote logout işlemi başarısız oldu.");
                }
            }, cancellationToken);
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
