using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Shared.Result;

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
            var validationResult = await _tokenValidationService.ValidateAndGetSessionAsync(request.AccessToken, request.RefreshToken);

            if (validationResult.IsFailure)
            {
                _logger.LogWarning("Logout için sağlanan refresh token zaten geçersizdi.");
                return Result.Success();
            }

            var session = validationResult.Value;

            try
            {
                session.Revoke();
                await _unitOfWork.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout sırasında oturum iptal edilirken beklenmedik bir hata oluştu. UserId: {UserId}", validationResult.Value);
                return Result.Success();
            }
        }
    }
}
