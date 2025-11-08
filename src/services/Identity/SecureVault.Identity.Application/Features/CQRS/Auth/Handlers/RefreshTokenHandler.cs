using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Identity.Application.Contracts.DTOs.AuthDto;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Application.Messages;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.Auth.Handlers
{
    public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RefreshTokenHandler> _logger;
        private readonly IStringLocalizer<ReturnMessages> _returnMessages;
        private readonly ITokenValidationService _tokenValidationService;
        public RefreshTokenHandler(IUserRepository userRepository, IJwtTokenService jwtTokenService, IUnitOfWork unitOfWork, ILogger<RefreshTokenHandler> logger, IStringLocalizer<ReturnMessages> returnMessages, ITokenValidationService tokenValidationService)
        {
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _returnMessages = returnMessages;
            _tokenValidationService = tokenValidationService;
        }

        public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var sessionValidationResult = await _tokenValidationService.ValidateAndGetSessionAsync(request.AccessToken, request.RefreshToken, request.DpopJkt);

            if (!sessionValidationResult.IsSuccess)
                return sessionValidationResult.Error;

            var session = sessionValidationResult.Value;

            try
            {
                var userWithInfo = await _userRepository.GetUserWithUserInfoAsync(session.UserId);
                var (newAccessToken, newAccessTokenJti, newAccessTokenExp) = _jwtTokenService.GenerateJwtTokenForUser(userWithInfo, request.DpopJkt);
                var (newRefreshToken, newRefreshTokenJti, newRefreshTokenExp) = _jwtTokenService.GenerateRefreshTokenJwt(session.UserId, session.IsPersistent, request.DpopJkt);


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
                _logger.LogError(ex, "An unexpected error occurred during the refresh token process. UserId: {UserId}", session.UserId); return new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]);
            }
        }
    }
}
