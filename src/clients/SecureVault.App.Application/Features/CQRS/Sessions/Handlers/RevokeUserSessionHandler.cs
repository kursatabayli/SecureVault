using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Interaction;
using SecureVault.App.Application.Features.CQRS.Sessions.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Sessions.Handlers;

public class RevokeUserSessionCommandHandler : IRequestHandler<RevokeUserSessionCommand, Result>
{
    private readonly IUserSessionService _userSessionService;
    private readonly IInteractionConnectionService _interactionConnectionService;
    private readonly ILogger<RevokeUserSessionCommandHandler> _logger;

    public RevokeUserSessionCommandHandler(IUserSessionService userSessionService, IInteractionConnectionService interactionConnectionService, ILogger<RevokeUserSessionCommandHandler> logger)
    {
        _userSessionService = userSessionService;
        _interactionConnectionService = interactionConnectionService;
        _logger = logger;
    }

    public async Task<Result> Handle(RevokeUserSessionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Revoke Session command started for SessionId: {SessionId}", request.SessionId);

        try
        {
            if (request.IsActiveNow)
            {
                _logger.LogInformation("Session {SessionId} is active; attempting SignalR notification.", request.SessionId);
                await _interactionConnectionService.NotifyUserSessionRevokedAsync(request.SessionId.ToString(), cancellationToken);
            }

            _logger.LogDebug("Calling API to logically revoke session {SessionId}...", request.SessionId);
            var result = await _userSessionService.LogoutAnyWhereAsync(request.SessionId, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Session {SessionId} successfully revoked via API.", request.SessionId);
            }
            else
            {
                _logger.LogWarning("API revocation failed for Session {SessionId}: {ErrorCode} - {ErrorMessage}",
                    request.SessionId, result.Error.Code, result.Error.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during session revocation for SessionId: {SessionId}", request.SessionId);
            return Result.Failure(new Error("Client.Revoke.Critical", "A critical error occurred while revoking the session."));
        }
    }
}
