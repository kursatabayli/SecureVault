using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Application.Contracts.Services;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Messages;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers
{
    public class DeleteVaultItemHandler : IRequestHandler<DeleteVaultItemCommand, Result>
    {
        private readonly IVaultItemsRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteVaultItemHandler> _logger;
        private readonly IStringLocalizer<ReturnMessages> _returnMessages;

        public DeleteVaultItemHandler(IUnitOfWork unitOfWork, ILogger<DeleteVaultItemHandler> logger, IStringLocalizer<ReturnMessages> returnMessages, IVaultItemsRepository repository)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _returnMessages = returnMessages;
            _repository = repository;
        }

        public async Task<Result> Handle(DeleteVaultItemCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var vaultItem = await _repository.GetByIdAsync(request.Id);

                if (vaultItem is null)
                {
                    _logger.LogWarning("Var olmayan bir vault item silinmeye çalışıldı. ItemId: {ItemId}", request.Id);
                    return Result.Failure(new Error(ErrorCodes.Vault.ItemNotFound, _returnMessages[ErrorCodes.Vault.ItemNotFound]));
                }

                if (vaultItem.UserId != request.UserId)
                {
                    _logger.LogCritical("YETKİSİZ ERİŞİM DENEMESİ: Kullanıcı {UserId}, kendisine ait olmayan bir item'ı ({ItemId}) silmeye çalıştı.", request.UserId, request.Id);
                    return Result.Failure(new Error(ErrorCodes.UnauthorizedAccess, _returnMessages[ErrorCodes.UnauthorizedAccess]));
                }

                vaultItem.Delete();
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Vault item başarıyla silindi. ItemId: {ItemId}, UserId: {UserId}", request.Id, request.UserId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                // Beklenmedik sistem hatası
                _logger.LogError(ex, "Vault item silinirken beklenmedik bir hata oluştu. ItemId: {ItemId}", request.Id);
                return Result.Failure(new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]));
            }
        }
    }
}
