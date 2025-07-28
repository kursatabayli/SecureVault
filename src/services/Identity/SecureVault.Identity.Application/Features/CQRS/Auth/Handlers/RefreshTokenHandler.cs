using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Identity.Application.Contracts.DTOs.AuthDto;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Application.Messages;
using SecureVault.Identity.Application.Services;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.Auth.Handlers
{
    public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RefreshTokenHandler> _logger;
        private readonly IStringLocalizer<ReturnMessages> _returnMessages;
        private readonly ITokenValidationService _tokenValidationService;
        public RefreshTokenHandler(IAuthService authService, IUnitOfWork unitOfWork, ILogger<RefreshTokenHandler> logger, IUserRepository userRepository, IStringLocalizer<ReturnMessages> returnMessages, ITokenValidationService tokenValidationService)
        {
            _authService = authService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userRepository = userRepository;
            _returnMessages = returnMessages;
            _tokenValidationService = tokenValidationService;
        }

        public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _tokenValidationService.ValidateAndGetSessionAsync(request.AccessToken, request.RefreshToken, request.UniqueDeviceId);

            var session = validationResult.Value;

            try
            {
                var userWithInfo = await _userRepository.GetUserWithUserInfoAsync(session.UserId);
                var (newAccessToken, newAccessTokenJti, newAccessTokenExp) = _authService.GenerateJwtTokenForUser(userWithInfo);
                var (newRefreshToken, newRefreshTokenJti, newRefreshTokenExp) = _authService.GenerateRefreshTokenJwt(session.UserId, session.IsPersistent);


                session.Update(
                    refreshTokenJti: newRefreshTokenJti,
                    accessTokenJti: newAccessTokenJti,
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
                _logger.LogError(ex, "Refresh token işlemi sırasında beklenmedik bir hata oluştu. UserId: {UserId}", session.UserId);
                return new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]);
            }
        }
    }
}
