using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Database;
using SecureVault.App.Application.Contracts.Abstractions.DPoP;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.UI;
using SecureVault.App.Application.Features.CQRS.Auth.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Auth.Handlers;

public class LogoutHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IAuthService _authService;
    private readonly IStorageService _storageService;
    private readonly IDpopKeyService _dpopKeyService;
    private readonly ILogger<LogoutHandler> _logger;
    private readonly IDatabaseManager _databaseManager;
    private readonly IAuthenticationStateNotifier _authenticationStateNotifier;

    public LogoutHandler(IAuthService authService, IStorageService storageService, IDpopKeyService dpopKeyService, ILogger<LogoutHandler> logger, IDatabaseManager databaseManager, IAuthenticationStateNotifier authenticationStateNotifier)
    {
        _authService = authService;
        _storageService = storageService;
        _dpopKeyService = dpopKeyService;
        _logger = logger;
        _databaseManager = databaseManager;
        _authenticationStateNotifier = authenticationStateNotifier;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logout process started...");

        try
        {
            await _authService.LogoutAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Local storage cleanup during logout failed, but continuing with remaining cleanup.");
        }

        try
        {
            _logger.LogInformation("Clearing in-memory DPoP key cache...");
            await _dpopKeyService.ClearDpopKeyCacheAsync();

            _logger.LogInformation("Cleaning up old database files from disk...");
            await _databaseManager.CleanupOldDbFilesAsync();

            _logger.LogInformation("Notifying UI to update authentication state...");
            await _authenticationStateNotifier.NotifyUserLogout();

            _logger.LogInformation("Local logout process completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "A critical error occurred while deleting local database files or clearing cache.");
            return Result.Failure(new Error("Database.Clear.Failed", "Failed to clear local data."));
        }
        return Result.Success();
    }
}
