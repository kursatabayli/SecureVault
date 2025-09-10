using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Application.Features.CQRS.Passwords.Handlers;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Commands;
using SecureVault.App.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Handlers
{
    public class CreateTwoFactorAuthCodeHandler : IRequestHandler<CreateTwoFactorAuthCodeCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateTwoFactorAuthCodeHandler> _logger;
        private readonly IBackgroundSyncService _backgroundSyncService;

        public CreateTwoFactorAuthCodeHandler(IUnitOfWork unitOfWork, ILogger<CreateTwoFactorAuthCodeHandler> logger, IBackgroundSyncService backgroundSyncService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _backgroundSyncService = backgroundSyncService;
        }

        public async Task<Result> Handle(CreateTwoFactorAuthCodeCommand request, CancellationToken cancellationToken)
        {
            var twoFactorAuthCodeEntity = TwoFactorAuthCodeEntity.Create(
                null,
                request.Issuer,
                request.AccountName,
                request.SecretKey,
                request.Type,
                request.Digits,
                request.Period,
                request.Counter,
                request.Algorithm,
                null, null, null);

            try
            {
                await _unitOfWork.TwoFactorAuthCodes.AddAsync(twoFactorAuthCodeEntity);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Parola ID:{Id} yerel veritabanına başarıyla kaydedildi. Senkronizasyon bekleniyor.", twoFactorAuthCodeEntity.Id);

                await _backgroundSyncService.SynchronizeAsync(cancellationToken);

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
