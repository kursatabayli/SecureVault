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

namespace SecureVault.App.Infrastructure.Services.Api;

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
        _logger.LogInformation("Login process started for email: {Email}", loginDto.Email);
        try
        {
            _logger.LogDebug("Fetching login challenge from API...");
            var challengeDto = await _secureVaultAnonymousApi.GetChallengeAsync(loginDto.Email, cancellationToken);
            _logger.LogDebug("Challenge received. Running client-side crypto in background thread...");

            var (privateKey, signatureHex, encryptionKey) = await Task.Run(() =>
            {
                var ms = _hashService.CreateMasterSecret(loginDto.Password, challengeDto.Salt);
                var pk = _hashService.GetPrivateKeyForAuth(ms, challengeDto.Salt);
                var sig = SignChallenge(challengeDto.Challenge, pk);
                var ek = _hashService.GetEncryptionKeyForData(ms, challengeDto.Salt);
                return (pk, sig, ek);
            }, cancellationToken);

            _logger.LogInformation("Client-side crypto complete. Sending login request to API...");
            var loginCredentials = new LoginCredentialsDto(loginDto.Email, signatureHex);
            var authResponse = await _secureVaultAnonymousApi.LoginAsync(loginDto.RememberMe, loginCredentials, cancellationToken);

            _logger.LogInformation("Login successful. Saving tokens and keys to storage...");
            await _storageService.SetTokensAsync(authResponse);
            await _storageService.SetKeysAsync(privateKey, encryptionKey);
            _storageService.SetEmail(loginDto.Email);
            _logger.LogInformation("Login process completed successfully for email: {Email}", loginDto.Email);

            return authResponse;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Login failed: Service is unavailable (Circuit Breaker is open).");
            return Result<AuthResponseDto>.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable. Please try again in a few minutes."));
        }
        catch (ApiException ex)
        {
            var error = await ex.GetContentAsAsync<Error>();
            _logger.LogError(ex, "Login failed: API returned error. Code: {ErrorCode}, Message: {ErrorMessage}", error?.Code, error?.Message);
            return Result<AuthResponseDto>.Failure(error ?? new Error("Client.LoginFailed", "Login failed."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Login failed: Could not connect to the server.");
            return Result<AuthResponseDto>.Failure(new Error("Service.ConnectionError", "Could not connect to the server. Please check your internet connection or try again later."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed: An unexpected client error occurred.");
            return Result<AuthResponseDto>.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred. Please try again later."));
        }
    }

    public async Task<Result<AuthResponseDto>> LoginWithQrCodeAsync(LoginQrCodeDto loginQrCodeDto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("QR Code Login process started for email: {Email}", loginQrCodeDto.Email);
        try
        {
            _logger.LogDebug("Fetching login challenge from API for QR login...");
            var challengeDto = await _secureVaultAnonymousApi.GetChallengeAsync(loginQrCodeDto.Email, cancellationToken);

            _logger.LogDebug("Challenge received. Signing challenge in background thread...");
            var signatureHex = await Task.Run(() =>
                SignChallenge(challengeDto.Challenge, loginQrCodeDto.PrivateKey),
                cancellationToken);

            _logger.LogInformation("Client-side signing complete. Sending QR login request to API...");
            var loginCredentials = new LoginCredentialsDto(loginQrCodeDto.Email, signatureHex);
            var authResponse = await _secureVaultAnonymousApi.LoginAsync(loginQrCodeDto.RememberMe, loginCredentials, cancellationToken);

            _logger.LogInformation("QR Login successful. Saving keys and tokens from QR payload...");
            await _storageService.SetTokensAsync(authResponse);
            await _storageService.SetKeysAsync(loginQrCodeDto.PrivateKey, loginQrCodeDto.EncryptionKey);
            _storageService.SetEmail(loginQrCodeDto.Email);
            _logger.LogInformation("QR Login process completed successfully for email: {Email}", loginQrCodeDto.Email);

            return authResponse;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "QR Login failed: Service is unavailable (Circuit Breaker is open).");
            return Result<AuthResponseDto>.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable. Please try again in a few minutes."));
        }
        catch (ApiException ex)
        {
            var error = await ex.GetContentAsAsync<Error>();
            _logger.LogError(ex, "QR Login failed: API returned error. Code: {ErrorCode}, Message: {ErrorMessage}", error?.Code, error?.Message);
            return Result<AuthResponseDto>.Failure(error ?? new Error("Client.LoginFailed", "Login failed."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "QR Login failed: Could not connect to the server.");
            return Result<AuthResponseDto>.Failure(new Error("Service.ConnectionError", "Could not connect to the server. Please check your internet connection or try again later."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QR Login failed: An unexpected client error occurred.");
            return Result<AuthResponseDto>.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred. Please try again later."));
        }
    }
    public async Task<Result?> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to refresh access token...");
        var refreshToken = await _storageService.GetRefreshTokenAsync();
        var refreshTokenExpiration = _storageService.GetRefreshTokenExpiration();

        if (string.IsNullOrEmpty(refreshToken) || refreshTokenExpiration <= DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token not found or expired. Forcing logout.");
            return Result.Failure(new Error("SessionExpired", "Session expired."));
        }

        try
        {
            _logger.LogDebug("Calling RefreshToken API endpoint...");
            var authResponse = await _secureVaultAnonymousApi.RefreshTokenAsync(refreshToken, cancellationToken);
            await _storageService.SetTokensAsync(authResponse);
            _logger.LogInformation("Access token refreshed and saved successfully.");
            return Result.Success();
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Token refresh failed: Service is unavailable (Circuit Breaker is open).");
            return Result.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable. Please try again in a few minutes."));
        }
        catch (ApiException ex)
        {
            var error = await ex.GetContentAsAsync<Error>();
            _logger.LogWarning(ex, "Token refresh failed: API returned error (e.g., refresh token revoked). Code: {ErrorCode}, Message: {ErrorMessage}", error?.Code, error?.Message);
            return Result.Failure(error ?? new Error("Auth.InvalidRefreshToken", "Token renewal failed."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Token refresh failed: Could not connect to the server.");
            return Result.Failure(new Error("Service.ConnectionError", "Could not connect to the server. Token not renewed."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed: An unexpected client error occurred.");
            return Result.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred during token renewal."));
        }
    }
    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting logout process...");

        var refreshToken = await _storageService.GetRefreshTokenAsync();
        var accessToken = await _storageService.GetAccessTokenAsync();

        if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(accessToken))
        {
            _logger.LogInformation("No tokens found in storage to send remote logout request (this is normal if already logged out).");
            return;
        }

        _logger.LogInformation("Dispatching background task for remote server logout...");
        _ = Task.Run(async () =>
        {
            try
            {
                await _secureVaultAnonymousApi.LogoutAsync(refreshToken, cancellationToken);
                _logger.LogInformation("Background remote logout task completed successfully.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Background remote logout task was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background remote logout task failed.");
            }
        }, cancellationToken);
    }


    //Login Helpers
    private string SignChallenge(string challenge, byte[] privateKeyBytes)
    {
        _logger.LogDebug("Signing challenge...");
        var msgBytes = Encoding.UTF8.GetBytes(challenge);
        var msgHash = SHA256.HashData(msgBytes);

        var signature = _bouncyCastleCryptoService.SignHash(msgHash, privateKeyBytes);
        _logger.LogDebug("Challenge signed successfully.");
        return signature;
    }
}
