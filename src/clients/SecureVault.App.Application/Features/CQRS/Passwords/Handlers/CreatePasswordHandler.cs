using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Interaction;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Application.Features.CQRS.Passwords.Commands;
using SecureVault.App.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Passwords.Handlers;

public class CreatePasswordHandler : IRequestHandler<CreatePasswordCommand, Result>
{
    private readonly IPasswordRepository _passwordRepository;
    private readonly ILogger<CreatePasswordHandler> _logger;
    private readonly IBackgroundSyncService _backgroundSyncService;
    private readonly IInteractionConnectionService _interactionConnectionService;

    public CreatePasswordHandler(
        IPasswordRepository passwordRepository,
        ILogger<CreatePasswordHandler> logger,
        IBackgroundSyncService backgroundSyncService,
        IInteractionConnectionService interactionConnectionService)
    {
        _passwordRepository = passwordRepository;
        _logger = logger;
        _backgroundSyncService = backgroundSyncService;
        _interactionConnectionService = interactionConnectionService;
    }

    public async Task<Result> Handle(CreatePasswordCommand request, CancellationToken cancellationToken)
    {
        var passwordEntity = new PasswordEntity
        {
            SiteName = request.SiteName,
            SiteUrl = request.SiteUrl,
            Username = request.Username,
            Password = request.Password,
            Notes = request.Notes,
        };

        try
        {
            await _passwordRepository.AddAsync(passwordEntity);

            _logger.LogInformation("Password ID:{Id} saved successfully to local DB. Queued for background sync.", passwordEntity.Id);

            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _backgroundSyncService.SynchronizeAsync(CancellationToken.None);

                    if (result)
                    {
                        _logger.LogInformation("Background sync for Password ID:{Id} completed successfully.", passwordEntity.Id);
                        await _interactionConnectionService.NotifySyncRequiredAsync(CancellationToken.None);
                    }
                    else
                    {
                        _logger.LogWarning("Background sync for Password ID:{Id} failed or was skipped.", passwordEntity.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background sync task failed for Password ID:{Id}", passwordEntity.Id);
                }
            }, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save password to local database.");
            var error = new Error("LOCAL_DB_SAVE_FAILED", "Could not save the password to your device.");
            return Result.Failure(error);
        }
    }
}
