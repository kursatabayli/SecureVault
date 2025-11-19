using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Commands;
using SecureVault.Identity.Application.Messages;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;
using System.Security.Claims;
using System.Text.Json;

namespace SecureVault.Identity.Application.Services;

public class TokenValidationService : ITokenValidationService
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly IStringLocalizer<ReturnMessages> _localizer;
    private readonly ILogger<TokenValidationService> _logger;

    public TokenValidationService(IJwtTokenService jwtTokenService, IUserSessionRepository userSessionRepository, IUnitOfWork unitOfWork, IMediator mediator, IStringLocalizer<ReturnMessages> localizer, ILogger<TokenValidationService> logger)
    {
        _jwtTokenService = jwtTokenService;
        _userSessionRepository = userSessionRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<Result<UserSession>> ValidateAndGetSessionAsync(string? accessToken, string? refreshToken, string? proofJkt = null)
    {
        var invalidAccessError = new Error("InvalidAccessToken", "Access token is invalid or missing.");
        var invalidRefreshError = new Error(ErrorCodes.Auth.InvalidRefreshToken, _localizer[ErrorCodes.Auth.InvalidRefreshToken]);

        if (string.IsNullOrEmpty(accessToken)) return invalidAccessError;
        var accessPrincipal = _jwtTokenService.GetPrincipalFromAccessToken(accessToken, validateLifetime: false);
        if (accessPrincipal == null || !Guid.TryParse(accessPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userIdFromAccessToken))
            return invalidAccessError;



        var refreshPrincipal = _jwtTokenService.GetPrincipalFromRefreshToken(refreshToken);
        if (refreshPrincipal is null)
        {
            _logger.LogWarning("Invalid refresh token provided (signature/expiration).");
            return invalidRefreshError;
        }

        if (!Guid.TryParse(refreshPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userIdFromRefreshToken) || string.IsNullOrEmpty(refreshPrincipal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value))
            return invalidRefreshError;

        var cnfClaim = refreshPrincipal.Claims.FirstOrDefault(c => c.Type == "cnf");
        var tokenJkt = GetJktFromCnf(cnfClaim?.Value);

        if (string.IsNullOrEmpty(tokenJkt))
        {
            _logger.LogWarning("Refresh token is not sender-constrained ('cnf' or 'jkt' claim missing). JTI: {Jti}", refreshPrincipal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value); return invalidRefreshError;
        }

        var jti = refreshPrincipal.FindFirst(JwtRegisteredClaimNames.Jti)!.Value;
        var matchingSession = await _userSessionRepository.GetSessionByJtiAsync(jti);

        if (matchingSession is null)
        {
            _logger.LogWarning("Refresh token validation failed: No session found for JTI: {Jti}", jti);
            return invalidRefreshError;
        }

        if (matchingSession.IsRevoked)
        {
            _logger.LogWarning("Refresh token validation failed: Session is revoked. JTI: {Jti}, SessionId: {SessionId}", jti, matchingSession.Id);
            return invalidRefreshError;
        }

        if (matchingSession.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token validation failed: Session has expired. JTI: {Jti}, SessionId: {SessionId}, Expiry: {Expiry}", jti, matchingSession.Id, matchingSession.ExpiresAt);
            return invalidRefreshError;
        }

        if (!string.Equals(proofJkt, tokenJkt, StringComparison.Ordinal))
        {
            _logger.LogCritical("DPoP JKT MISMATCH! Potential stolen refresh token. Proof JKT: {ProofJkt}, Token JKT: {TokenJkt}. User: {UserId}, Session: {SessionId}. REVOKING SESSION.", proofJkt, tokenJkt, matchingSession.UserId, matchingSession.Id);
            var result = await _mediator.Send(new RevokeSessionCommand(matchingSession.Id, matchingSession.UserId));
            if (result.IsFailure)
            {
                matchingSession.Revoke();
                await _unitOfWork.SaveChangesAsync();
            }
            return invalidRefreshError;
        }

        if (matchingSession.UserId != userIdFromAccessToken || userIdFromAccessToken != userIdFromRefreshToken)
        {
            _logger.LogWarning("Token-Session mismatch! AccessTokenId: {AccessTokenUserId}, RefreshTokenId: {RefreshTokenUserId}, SessionUserId: {SessionUserId}", userIdFromAccessToken, userIdFromRefreshToken, matchingSession.UserId);
            return invalidRefreshError;
        }

        _logger.LogInformation("Access and Refresh token validation successful. User: {UserId}, SessionId: {SessionId}", matchingSession.UserId, matchingSession.Id);

        return matchingSession;
    }

    private string? GetJktFromCnf(string? cnfJson)
    {
        if (string.IsNullOrEmpty(cnfJson))
            return null;

        try
        {
            using var jsonDoc = JsonDocument.Parse(cnfJson);
            if (jsonDoc.RootElement.TryGetProperty("jkt", out var jktElement))
            {
                return jktElement.GetString();
            }
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse 'cnf' claim JSON: {CnfJson}", cnfJson);
            return null;
        }
    }
}
