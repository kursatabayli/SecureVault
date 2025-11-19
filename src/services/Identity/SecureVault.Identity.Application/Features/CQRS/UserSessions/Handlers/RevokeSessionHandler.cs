using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Commands;
using SecureVault.Identity.Application.Messages;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.UserSessions.Handlers;

public class RevokeSessionHandler : IRequestHandler<RevokeSessionCommand, Result>
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevokeSessionHandler> _logger;
    private readonly IStringLocalizer<ReturnMessages> _returnMessages;
    private readonly ICacheService _cacheService;

    public RevokeSessionHandler(IUserSessionRepository sessionRepository, IUnitOfWork unitOfWork, ILogger<RevokeSessionHandler> logger, IStringLocalizer<ReturnMessages> returnMessages, ICacheService cacheService)
    {
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _returnMessages = returnMessages;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var sessionToRevoke = await _sessionRepository.GetByIdAsync(request.SessionId);

            if (sessionToRevoke is null || sessionToRevoke.IsRevoked)
            {
                _logger.LogWarning("Attempted to revoke a non-existent or already revoked session. SessionId: {SessionId}", request.SessionId);
                return Result.Success();
            }

            if (sessionToRevoke.UserId != request.CurrentUserId)
            {
                _logger.LogCritical("SECURITY ALERT: Unauthorized session revocation attempt! ActorId: {ActorId}, TargetOwnerId: {TargetId}", request.CurrentUserId, sessionToRevoke.UserId);

                return Result.Failure(new Error(ErrorCodes.UnauthorizedAccess, _returnMessages[ErrorCodes.UnauthorizedAccess]));
            }
            var accessTokenJti = sessionToRevoke.AccessTokenJti;
            if (!string.IsNullOrEmpty(accessTokenJti))
            {
                var cacheKey = $"blacklist:{accessTokenJti}";

                var approxAccessTokenExpiryTime = sessionToRevoke.LastUsedAt.GetValueOrDefault(DateTime.UtcNow).AddMinutes(15);
                var expiryTimeSpan = approxAccessTokenExpiryTime - DateTime.UtcNow;

                if (expiryTimeSpan > TimeSpan.Zero)
                {
                    _logger.LogInformation(
                        "Blacklisting JTI {Jti} in cache for {ExpiryTime} to prevent token reuse. SessionId: {SessionId}",
                        accessTokenJti, expiryTimeSpan, request.SessionId);

                    await _cacheService.SetAsync(cacheKey, "revoked", expiryTimeSpan);
                }
                else
                {
                    _logger.LogInformation(
                        "JTI {Jti} not blacklisted as its associated access token is already expired. SessionId: {SessionId}",
                        accessTokenJti, request.SessionId);
                }
            }

            sessionToRevoke.Revoke();
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Session revoked successfully. SessionId: {SessionId}, UserId: {UserId}", request.SessionId, request.CurrentUserId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during session revocation. SessionId: {SessionId}", request.SessionId);
            return Result.Failure(new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]));
        }
    }
}
