using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Interaction;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Commands;
using SecureVault.App.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Handlers;

public class CreateTwoFactorAuthCodeHandler : IRequestHandler<CreateTwoFactorAuthCodeCommand, Result>
{
    private readonly ITwoFactorAuthCodeRepository _repository;
    private readonly ILogger<CreateTwoFactorAuthCodeHandler> _logger;
    private readonly IBackgroundSyncService _backgroundSyncService;
    private readonly IInteractionConnectionService _interactionConnectionService;

    public CreateTwoFactorAuthCodeHandler(
        ITwoFactorAuthCodeRepository repository,
        ILogger<CreateTwoFactorAuthCodeHandler> logger,
        IBackgroundSyncService backgroundSyncService,
        IInteractionConnectionService interactionConnectionService)
    {
        _repository = repository;
        _logger = logger;
        _backgroundSyncService = backgroundSyncService;
        _interactionConnectionService = interactionConnectionService;
    }

    public async Task<Result> Handle(CreateTwoFactorAuthCodeCommand request, CancellationToken cancellationToken)
    {
        var twoFactorAuthCodeEntity = new TwoFactorAuthCodeEntity
        {
            Issuer = request.Issuer,
            AccountName = request.AccountName,
            SecretKey = request.SecretKey,
            Type = request.Type,
            Digits = request.Digits,
            Period = request.Period,
            Counter = request.Counter,
            Algorithm = request.Algorithm,
        };

        try
        {
            await _repository.AddAsync(twoFactorAuthCodeEntity);

            _logger.LogInformation("2FA Code ID:{Id} saved successfully to local DB. Queued for background sync.", twoFactorAuthCodeEntity.Id);

            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _backgroundSyncService.SynchronizeAsync(CancellationToken.None);

                    if (result)
                    {
                        _logger.LogInformation("Background sync for 2FA Code ID:{Id} completed successfully.", twoFactorAuthCodeEntity.Id);
                        await _interactionConnectionService.NotifySyncRequiredAsync(CancellationToken.None);
                    }
                    else
                    {
                        _logger.LogWarning("Background sync for 2FA Code ID:{Id} failed or was skipped.", twoFactorAuthCodeEntity.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background sync task failed for 2FA Code ID:{Id}", twoFactorAuthCodeEntity.Id);
                }
            }, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save 2FA Code to local database.");
            var error = new Error("LOCAL_DB_SAVE_FAILED", "Could not save the 2FA Code to your device.");
            return Result.Failure(error);
        }
    }
}
