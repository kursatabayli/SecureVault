using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Refit;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.DTOs.Session;
using SecureVault.Shared.Result;
using System.Threading;

namespace SecureVault.App.Infrastructure.Services.Api;

public class UserSessionService : IUserSessionService
{
    private readonly ISecureVaultAuthorizeApi _secureVaultApi;
    private readonly ILogger<UserSessionService> _logger;

    public UserSessionService(ISecureVaultAuthorizeApi secureVaultApi, ILogger<UserSessionService> logger)
    {
        _secureVaultApi = secureVaultApi;
        _logger = logger;
    }

    public async Task<Result<List<UserSessionsDto>>> GetUserSessionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Attempting to get user sessions from API...");
            var sessions = await _secureVaultApi.GetUserSessionsAsync(cancellationToken);

            _logger.LogInformation("Successfully retrieved {Count} user sessions.", sessions.Count);
            return sessions;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Get user sessions failed: Circuit Breaker is open.");
            return Result<List<UserSessionsDto>>.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable. Please try again in a few minutes."));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Get user sessions failed: API error. Status Code: {StatusCode}", ex.StatusCode);
            var error = await ex.GetContentAsAsync<Error>();
            return Result<List<UserSessionsDto>>.Failure(error ?? new Error("Api.RequestFailed", "Failed to load sessions."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Get user sessions failed: Network error.");
            return Result<List<UserSessionsDto>>.Failure(new Error("Service.ConnectionError", "Could not connect to the server. Please check your internet connection."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get user sessions failed: Unexpected error.");
            return Result<List<UserSessionsDto>>.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred."));
        }
    }

    public async Task<Result> LogoutAnyWhereAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Attempting to revoke (logout) session: {SessionId}", sessionId);
            var response = await _secureVaultApi.LogoutSessionAsync(sessionId, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully revoked session: {SessionId}", sessionId);
                return Result.Success();
            }

            var error = await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.DeleteFailed", "Failed to revoke session.");
            _logger.LogWarning("Failed to revoke session {SessionId} (API business logic error): {ErrorCode} - {ErrorMessage}",
                sessionId, error.Code, error.Message);

            return Result.Failure(error);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Revoke session failed: Circuit Breaker is open. SessionId: {SessionId}", sessionId);
            return Result.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable. Please try again in a few minutes."));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Revoke session failed: API error. SessionId: {SessionId}, Status Code: {StatusCode}", sessionId, ex.StatusCode);
            var error = await ex.GetContentAsAsync<Error>();
            return Result.Failure(error ?? new Error("Client.DeleteFailed", "Failed to revoke session."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Revoke session failed: Network error. SessionId: {SessionId}", sessionId);
            return Result.Failure(new Error("Service.ConnectionError", "Could not connect to the server. Please check your internet connection."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Revoke session failed: Unexpected error. SessionId: {SessionId}", sessionId);
            return Result.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred."));
        }
    }
}
