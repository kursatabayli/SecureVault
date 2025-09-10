using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Application.Features.CQRS.Passwords.Commands;
using SecureVault.App.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Passwords.Handlers
{
    public class CreatePasswordHandler : IRequestHandler<CreatePasswordCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreatePasswordHandler> _logger;
        private readonly IBackgroundSyncService _backgroundSyncService;

        public CreatePasswordHandler(
            IUnitOfWork unitOfWork,
            ILogger<CreatePasswordHandler> logger,
            IBackgroundSyncService backgroundSyncService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _backgroundSyncService = backgroundSyncService;
        }

        public async Task<Result> Handle(CreatePasswordCommand request, CancellationToken cancellationToken)
        {
            var passwordEntity = PasswordEntity.Create(
                null,
                request.SiteName,
                request.SiteUrl,
                request.Username,
                request.Password,
                request.Notes,
                null, null, null);

            try
            {
                await _unitOfWork.Passwords.AddAsync(passwordEntity);
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Parola ID:{Id} yerel veritabanına başarıyla kaydedildi. Senkronizasyon bekleniyor.", passwordEntity.Id);

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

        //public async Task<Result> Handle(CreatePasswordCommand request, CancellationToken cancellationToken)
        //{
        //    var mappedPasswordDto = _mapper.Map<PasswordDto>(request);

        //    var passwordEntity = PasswordEntity.Create(
        //        null,
        //        request.SiteName,
        //        request.SiteUrl,
        //        request.Username,
        //        request.Password,
        //        request.Notes,
        //        null,
        //        null,
        //        null);

        //    var encryptionKey = await _storageService.GetEncryptionKeyAsByteAsync();
        //    var encryptedData = _cryptoService.Encrypt(mappedPasswordDto, encryptionKey);
        //    CreateVaultItemDto createVaultItemDto = new()
        //    {
        //        Id = passwordEntity.Id,
        //        ItemType = ItemType.Password,
        //        EncryptedData = encryptedData,
        //        CreatedAt = passwordEntity.CreatedAt,
        //    };
        //    var lastSyncDate = _storageService.GetLastSyncDate();
        //    _storageService.SetLastSyncDate(passwordEntity.UpdatedAt);
        //    var remoteResult = await _vaultItemService.CreateVaultItem(createVaultItemDto);

        //    if (!remoteResult.IsSuccess)
        //    {
        //        _logger.LogWarning("Uzak sunucuya parola kaydı başarısız oldu: {Error}", remoteResult.Error.Message);
        //        _storageService.SetLastSyncDate(lastSyncDate);
        //        return remoteResult;
        //    }

        //    _logger.LogInformation("Parola uzak sunucuya başarıyla kaydedildi.");

        //    try
        //    {
        //        await _unitOfWork.Passwords.AddAsync(passwordEntity);
        //        await _unitOfWork.CompleteAsync();
        //        _logger.LogInformation("Parola yerel veritabanına başarıyla kaydedildi.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Sunucuya kayıt başarılı olmasına rağmen yerel veritabanına kayıt sırasında hata oluştu. Veri tutarsızlığı olabilir!");
        //        var error = new Error("LOCAL_DB_SAVE_FAILED", "Veri sunucuya kaydedildi ancak yerel depolamada bir sorun oluştu.");
        //        _storageService.SetLastSyncDate(lastSyncDate);
        //        return Result.Failure(error);
        //    }

        //    return Result.Success();
        //}
    }
}
