using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Messages;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;
using System.Security.Claims;

namespace SecureVault.Identity.Application.Services
{
    public class TokenValidationService : ITokenValidationService
    {
        private readonly IAuthService _authService;
        private readonly IUserSessionRepository _userSessionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<ReturnMessages> _localizer;
        private readonly ILogger<TokenValidationService> _logger;

        public TokenValidationService(IAuthService authService, IUserSessionRepository userSessionRepository, IUnitOfWork unitOfWork, IStringLocalizer<ReturnMessages> localizer, ILogger<TokenValidationService> logger)
        {
            _authService = authService;
            _userSessionRepository = userSessionRepository;
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _logger = logger;
        }

        public async Task<Result<UserSession>> ValidateAndGetSessionAsync(string? accessToken, string? refreshToken, string? uniqueDeviceId = null)
        {
            var invalidAccessError = new Error("InvalidAccessToken", "Access token is invalid or missing.");
            var invalidRefreshError = new Error(ErrorCodes.Auth.InvalidRefreshToken, _localizer[ErrorCodes.Auth.InvalidRefreshToken]);

            if (string.IsNullOrEmpty(accessToken)) return invalidAccessError;
            var accessPrincipal = _authService.GetPrincipalFromAccessToken(accessToken, validateLifetime: false);
            if (accessPrincipal == null || !Guid.TryParse(accessPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userIdFromAccessToken))
                return invalidAccessError;



            var refreshPrincipal = _authService.GetPrincipalFromRefreshToken(refreshToken);
            if (refreshPrincipal is null)
            {
                _logger.LogWarning("Geçersiz refresh token sağlandı (imza/süre).");
                return invalidRefreshError;
            }

            if (!Guid.TryParse(refreshPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userIdFromRefreshToken) || string.IsNullOrEmpty(refreshPrincipal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value))
                return invalidRefreshError;

            var jti = refreshPrincipal.FindFirst(JwtRegisteredClaimNames.Jti)!.Value;
            var matchingSession = await _userSessionRepository.GetSessionByJtiAsync(jti);

            if (matchingSession is null) return invalidRefreshError;

            if (matchingSession.UserId != userIdFromAccessToken || userIdFromAccessToken != userIdFromRefreshToken)
            {
                _logger.LogWarning("Token-Oturum uyuşmazlığı! AccessTokenId: {AccessTokenUserId}, RefreshTokenId: {RefreshTokenUserId}, SessionUserId: {SessionUserId}",
                    userIdFromAccessToken, userIdFromRefreshToken, matchingSession.UserId);
                return invalidRefreshError;
            }

            if (uniqueDeviceId is not null && matchingSession.DeviceDetails.UniqueDeviceId != uniqueDeviceId)
            {
                _logger.LogCritical("GÜVENLİK UYARISI: Çalınan refresh token kullanıldı! Kullanıcı: {UserId}, Oturum: {SessionId}. Oturum iptal ediliyor.", matchingSession.UserId, matchingSession.Id);
                matchingSession.Revoke();
                await _unitOfWork.SaveChangesAsync();
                return invalidRefreshError;
            }

            return matchingSession;
        }
    }
}
