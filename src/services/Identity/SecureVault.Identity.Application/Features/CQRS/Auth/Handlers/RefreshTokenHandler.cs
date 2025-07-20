using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using SecureVault.Identity.Application.Contracts.DTOs.AuthDto;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Application.Messages;
using SecureVault.Identity.Application.Services;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;
using System.Security.Claims;

namespace SecureVault.Identity.Application.Features.CQRS.Auth.Handlers
{
    public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
    {
        private readonly IUserSessionRepository _userSessionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RefreshTokenHandler> _logger;
        private readonly IStringLocalizer<ReturnMessages> _returnMessages;
        private readonly ITokenValidationService _tokenValidationService;
        public RefreshTokenHandler(IUserSessionRepository userSessionRepository, IAuthService authService, IUnitOfWork unitOfWork, ILogger<RefreshTokenHandler> logger, IUserRepository userRepository, IStringLocalizer<ReturnMessages> returnMessages, ITokenValidationService tokenValidationService)
        {
            _userSessionRepository = userSessionRepository;
            _authService = authService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userRepository = userRepository;
            _returnMessages = returnMessages;
            _tokenValidationService = tokenValidationService;
        }

        public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _tokenValidationService.ValidateRefreshTokenAndGetSessionAsync(request.RefreshToken, request.UniqueDeviceId);

            var (session, userId) = validationResult.Value;

            try
            {
                var userWithInfo = await _userRepository.GetUserWithUserInfoAsync(userId);
                var (newAccessToken, newAccessTokenExp) = _authService.GenerateJwtTokenForUser(userWithInfo);
                var (newRefreshToken, newJti, newRefreshTokenExp) = _authService.GenerateRefreshTokenJwt(userId, session.IsPersistent);

                session.Update(
                    tokenIdentifier: newJti,
                    ipAddress: request.IpAddress,
                    expiresAt: newRefreshTokenExp
                );

                await _unitOfWork.SaveChangesAsync();

                return new AuthResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    AccessTokenExpiration = newAccessTokenExp,
                    RefreshTokenExpiration = newRefreshTokenExp
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token işlemi sırasında beklenmedik bir hata oluştu. UserId: {UserId}", userId);
                return new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]);
            }
        }
    }
}
