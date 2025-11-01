using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Commands;
using SecureVault.App.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Handlers
{
    public class CreateTwoFactorAuthCodeHandler : IRequestHandler<CreateTwoFactorAuthCodeCommand, Result>
    {
        private readonly ITwoFactorAuthCodeRepository _repository;
        private readonly ILogger<CreateTwoFactorAuthCodeHandler> _logger;
        private readonly IBackgroundSyncService _backgroundSyncService;
        private readonly ISyncConnectionService _syncConnectionService;

        public CreateTwoFactorAuthCodeHandler(
            ITwoFactorAuthCodeRepository repository,
            ILogger<CreateTwoFactorAuthCodeHandler> logger,
            IBackgroundSyncService backgroundSyncService,
            ISyncConnectionService syncConnectionService)
        {
            _repository = repository;
            _logger = logger;
            _backgroundSyncService = backgroundSyncService;
            _syncConnectionService = syncConnectionService;
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
                _logger.LogInformation("2FA Kodu ID:{Id} yerel veritabanına başarıyla kaydedildi. Senkronizasyon bekleniyor.", twoFactorAuthCodeEntity.Id);
                var result = await _backgroundSyncService.SynchronizeAsync(cancellationToken);
                if (result)
                {
                    _logger.LogInformation("2FA Kodu ID:{Id} için arka plan senkronizasyonu başarıyla tamamlandı.", twoFactorAuthCodeEntity.Id);
                    await _syncConnectionService.NotifySyncRequiredAsync(cancellationToken);
                }
                else
                {
                    _logger.LogWarning("2FA Kodu ID:{Id} için arka plan senkronizasyonu başarısız oldu veya atlandı.", twoFactorAuthCodeEntity.Id);
                }
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "2FA Kodu yerel veritabanına kaydedilirken bir hata oluştu.");
                var error = new Error("LOCAL_DB_SAVE_FAILED", "2FA Kodu cihazınıza kaydedilemedi.");
                return Result.Failure(error);
            }
        }
    }
}
