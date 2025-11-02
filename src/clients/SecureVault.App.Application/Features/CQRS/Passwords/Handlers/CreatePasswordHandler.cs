using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Interaction;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Application.Features.CQRS.Passwords.Commands;
using SecureVault.App.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Passwords.Handlers
{
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

                _logger.LogInformation("Parola ID:{Id} yerel veritabanına başarıyla kaydedildi. Senkronizasyon bekleniyor.", passwordEntity.Id);

                var result = await _backgroundSyncService.SynchronizeAsync(cancellationToken);
                if (result)
                {
                    _logger.LogInformation("Parola ID:{Id} için arka plan senkronizasyonu başarıyla tamamlandı.", passwordEntity.Id);
                    await _interactionConnectionService.NotifySyncRequiredAsync(cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Parola ID:{Id} için arka plan senkronizasyonu başarısız oldu veya atlandı.", passwordEntity.Id);
                }
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parola yerel veritabanına kaydedilirken bir hata oluştu.");
                var error = new Error("LOCAL_DB_SAVE_FAILED", "Parola cihazınıza kaydedilemedi.");
                return Result.Failure(error);
            }
        }
    }
}
