using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Refit;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.DTOs.Register;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Api;

public class RegisterService : IRegisterService
{
    private readonly ISecureVaultAnonymousApi _secureVaultAnonymousApi;
    private readonly ILogger<RegisterService> _logger;

    public RegisterService(ISecureVaultAnonymousApi secureVaultAnonymousApi, ILogger<RegisterService> logger)
    {
        _secureVaultAnonymousApi = secureVaultAnonymousApi;
        _logger = logger;
    }

    public async Task<Result?> RegisterAsync(RegisterUserDto registerUserDto, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _secureVaultAnonymousApi.RegisterAsync(registerUserDto, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("User registration successful for email: {Email}", registerUserDto.Email);
                return Result.Success();
            }

            var error = await response.Error.GetContentAsAsync<Error>();
            _logger.LogWarning("User registration failed (API business logic error) for email: {Email}. Error: {ErrorCode} - {ErrorMessage}",
                registerUserDto.Email, error?.Code, error?.Message);

            return Result.Failure(error ?? new Error("Client.RegisterFailed", "Registration failed."));
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Registration failed: Circuit Breaker is open. Email: {Email}", registerUserDto.Email);
            return Result.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable. Please try again in a few minutes."));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Registration failed: API error. Email: {Email}, Status Code: {StatusCode}", registerUserDto.Email, ex.StatusCode);
            var error = await ex.GetContentAsAsync<Error>();
            return Result.Failure(error ?? new Error("Api.RequestFailed", "Registration request failed."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Registration failed: Network error. Email: {Email}", registerUserDto.Email);
            return Result.Failure(new Error("Service.ConnectionError", "Could not connect to the server. Please check your internet connection or try again later."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during registration. Email: {Email}", registerUserDto.Email);
            return Result.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred. Please try again later."));
        }
    }
}
