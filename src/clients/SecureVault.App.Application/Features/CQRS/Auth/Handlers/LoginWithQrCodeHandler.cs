using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.Abstractions.UI;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Features.CQRS.Auth.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Auth.Handlers;

internal class LoginWithQrCodeHandler : IRequestHandler<LoginWithQrCodeCommand, Result>
{
    private readonly IAuthService _authService;
    private readonly IMapper _mapper;
    private readonly ILocalVaultService _localVaultService;
    private readonly ISyncLock _syncLock;
    private readonly IAuthenticationStateNotifier _authenticationStateNotifier;
    private readonly ILogger<LoginWithQrCodeHandler> _logger;

    public LoginWithQrCodeHandler(IAuthService authService, IMapper mapper, ILocalVaultService localVaultService, ISyncLock syncLock, IAuthenticationStateNotifier authenticationStateNotifier, ILogger<LoginWithQrCodeHandler> logger)
    {
        _authService = authService;
        _mapper = mapper;
        _localVaultService = localVaultService;
        _syncLock = syncLock;
        _authenticationStateNotifier = authenticationStateNotifier;
        _logger = logger;
    }

    public async Task<Result> Handle(LoginWithQrCodeCommand request, CancellationToken cancellationToken)
    {
        var loginDto = _mapper.Map<LoginQrCodeDto>(request);

        var loginResult = await _authService.LoginWithQrCodeAsync(loginDto, cancellationToken);

        if (loginResult.IsFailure)
        {
            _logger.LogWarning("QR Code Login failed at AuthService stage: {ErrorCode} - {ErrorMessage}",
                loginResult.Error.Code, loginResult.Error.Message);
            return loginResult;
        }

        _logger.LogInformation("Login successful, acquiring sync lock for initial data pull...");
        await using var lockHandle = await _syncLock.AcquireLockAsync(TimeSpan.FromMinutes(1), cancellationToken);
        if (!lockHandle.IsAcquired)
        {
            return Result.Failure(new Error("Client.Login.LockFailed", "System is busy, please try again."));
        }

        try
        {
            _logger.LogInformation("Notifying AuthState change...");
            await _authenticationStateNotifier.NotifyUserAuthenticated(loginResult.Value.AccessToken);

            _logger.LogInformation("Starting initial vault data pull (SetAllVaultDataAsync)...");
            var setVaultResult = await _localVaultService.SetAllVaultDataAsync(cancellationToken);

            if (setVaultResult.IsFailure)
            {
                _logger.LogError("Initial vault data pull FAILED: {Error}", setVaultResult.Error);
                return Result.Failure(setVaultResult.Error);
            }

            _logger.LogInformation("Initial vault data pull completed successfully.");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Login-Sync orchestration.");
            return Result.Failure(new Error("Client.Login.Critical", "A critical error occurred."));
        }
    }
}
