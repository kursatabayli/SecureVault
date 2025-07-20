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

        public async Task<Result<(UserSession session, Guid userId)>> ValidateRefreshTokenAndGetSessionAsync(string refreshToken, string? uniqueDeviceId = null)
        {
            var errorResult = new Error(ErrorCodes.Auth.InvalidRefreshToken, _localizer[ErrorCodes.Auth.InvalidRefreshToken]);

            var userClaims = _authService.GetPrincipalFromRefreshToken(refreshToken);
            if (userClaims is null)
            {
                _logger.LogWarning("Geçersiz refresh token sağlandı (imza/süre).");
                return errorResult;
            }
            if (!Guid.TryParse(userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            {
                _logger.LogWarning("Refresh token geçerli bir kullanıcı ID'si içermiyor.");
                return errorResult;
            }
            var jti = userClaims.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti))
            {
                _logger.LogWarning("Refresh token (Kullanıcı: {UserId}) JTI bilgisi içermiyor.", userId);
                return errorResult;
            }
            var matchingSession = await _userSessionRepository.GetSessionByJtiAsync(jti);
            if (matchingSession is null || matchingSession.IsRevoked)
            {
                _logger.LogWarning("Refresh token (Kullanıcı: {UserId}) için eşleşen veya geçerli bir oturum bulunamadı.", userId);
                return errorResult;
            }
            if (uniqueDeviceId is not null && matchingSession.DeviceDetails.UniqueDeviceId != uniqueDeviceId)
            {
                _logger.LogCritical("GÜVENLİK UYARISI: Çalınan refresh token kullanıldı! Kullanıcı: {UserId}, Oturum: {SessionId}. Oturum iptal ediliyor.", userId, matchingSession.Id);
                matchingSession.Revoke();
                await _unitOfWork.SaveChangesAsync();
                return errorResult;
            }

            return (matchingSession, userId);
        }
    }
}
