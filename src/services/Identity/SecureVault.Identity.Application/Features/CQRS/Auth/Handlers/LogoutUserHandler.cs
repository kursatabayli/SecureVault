using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.Auth.Handlers;

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
        var validationResult = await _tokenValidationService.ValidateAndGetSessionAsync(request.AccessToken, request.RefreshToken, request.DpopJkt);

        if (validationResult.IsFailure)
        {
            _logger.LogWarning("Refresh token provided for logout was already invalid (or DPoP proof was missing/mismatched).");
            return Result.Success();
        }

        var session = validationResult.Value;

        try
        {
            session.Revoke();
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User logged out successfully. Session revoked. UserId: {UserId}, SessionId: {SessionId}", session.UserId, session.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while revoking the session during logout. UserId: {UserId}, SessionId: {SessionId}", session.UserId, session.Id);
            return Result.Success();
        }
    }
}
