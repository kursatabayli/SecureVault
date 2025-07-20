using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Application.Messages;
using SecureVault.Identity.Application.Services;
using SecureVault.Shared.Result;
using System.Security.Claims;

namespace SecureVault.Identity.Application.Features.CQRS.Auth.Handlers
{
    public class LogoutUserHandler : IRequestHandler<LogoutUserCommand, Result>
    {
        private readonly ITokenValidationService _tokenValidationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LogoutUserHandler> _logger;

        public LogoutUserHandler(ITokenValidationService tokenValidationService, IUnitOfWork unitOfWork, ILogger<LogoutUserHandler> logger)
        {
            _tokenValidationService = tokenValidationService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _tokenValidationService.ValidateRefreshTokenAndGetSessionAsync(request.RefreshToken);

            if (validationResult.IsFailure)
            {
                _logger.LogWarning("Logout için sağlanan refresh token zaten geçersizdi.");
                return Result.Success();
            }

            var session = validationResult.Value.session;

            try
            {
                session.Revoke();
                await _unitOfWork.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout sırasında oturum iptal edilirken beklenmedik bir hata oluştu. UserId: {UserId}", validationResult.Value.userId);
                return Result.Success();
            }
        }
    }
}
