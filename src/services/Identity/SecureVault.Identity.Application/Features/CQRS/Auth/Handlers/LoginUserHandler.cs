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
    public class LoginUserHandler : IRequestHandler<LoginUserCommand, Result<AuthResponse>>
    {
        private readonly IMediator _mediator;
        private readonly IUserRepository _userRepository;
        private readonly IUserSessionService _userSessionService;
        private readonly IAuthService _authService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LoginUserHandler> _logger;
        private readonly IStringLocalizer<ReturnMessages> _returnMessages;
        public LoginUserHandler(IMediator mediator, IUserRepository userRepository, IAuthService authService, IUnitOfWork unitOfWork, IStringLocalizer<ReturnMessages> returnMessages, ILogger<LoginUserHandler> logger, IUserSessionService userSessionService)
        {
            _mediator = mediator;
            _userRepository = userRepository;
            _authService = authService;
            _unitOfWork = unitOfWork;
            _returnMessages = returnMessages;
            _logger = logger;
            _userSessionService = userSessionService;
        }

        public async Task<Result<AuthResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var verificationCommand = new VerifyLoginSignatureCommand(request.Email, request.Signature);
            var verificationResult = await _mediator.Send(verificationCommand, cancellationToken);

            if (verificationResult.IsFailure)
                return verificationResult.Error;

            try
            {
                var user = verificationResult.Value;

                var userWithInfo = await _userRepository.GetUserWithUserInfoAsync(user.Id);
                var (accessToken, accessTokenExp) = _authService.GenerateJwtTokenForUser(userWithInfo);
                var (newRefreshToken, newJti, newRefreshTokenExp) = _authService.GenerateRefreshTokenJwt(user.Id, request.RememberMe);

                await _userSessionService.ManageSessionAsync(user, request, newJti, newRefreshTokenExp);

                await _unitOfWork.SaveChangesAsync();

                return new AuthResponse
                {
                    AccessToken = accessToken,
                    AccessTokenExpiration = accessTokenExp,
                    RefreshToken = newRefreshToken,
                    RefreshTokenExpiration = newRefreshTokenExp
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login işlemi sırasında token oluşturma veya session kaydında beklenmedik bir hata oluştu. UserId: {UserId}", verificationResult.Value.Id);
                return new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]);
            }
        }
    }
}
