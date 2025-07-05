using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.RepositoryContracts;
using SecureVault.Vault.Application.Contracts.ServicesContracts;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Messages;
using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers
{
    public class UpdateVaultItemHandler : IRequestHandler<UpdateVaultItemCommand, Result>
    {
        private readonly IVaultItemsRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateVaultItemHandler> _logger;
        private readonly IStringLocalizer<ReturnMessages> _returnMessages;

        public UpdateVaultItemHandler(IUnitOfWork unitOfWork, ILogger<UpdateVaultItemHandler> logger, IStringLocalizer<ReturnMessages> returnMessages, IVaultItemsRepository repository)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _returnMessages = returnMessages;
            _repository = repository;
        }

        public async Task<Result> Handle(UpdateVaultItemCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var vaultItem = await _repository.GetByIdAsync(request.Id);

                string itemType = request.ItemType switch
                {
                    ItemType.Password => _returnMessages[ReturnMessages.ItemType_Password],
                    ItemType.TwoFactorAuth => _returnMessages[ReturnMessages.ItemType_TwoFactorAuth],
                    ItemType.CreditCard => _returnMessages[ReturnMessages.ItemType_CreditCard],
                    _ => throw new NotImplementedException(),
                };

                if (vaultItem == null)
                {
                    Error error = new(nameof(ErrorCode.VaultItemNotFound), _returnMessages[ReturnMessages.Error_ItemNotFound, itemType]);
                    _logger.LogWarning(error.Message);
                    return Result.Failure(error);
                }

                if (vaultItem.UserId != request.UserId)
                {
                    Error error = new(nameof(ErrorCode.AuthorizationFailure), _returnMessages[ReturnMessages.Error_Unauthorized]);
                    _logger.LogWarning(error.Message);
                    return Result.Failure(error);
                }

                vaultItem.UpdateData(request.EncryptedData);
                await _unitOfWork.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VaultItem güncellenirken bir hata oluştu. Id: {VaultItemId}", request.Id);
                return Result.Failure(new Error("InternalError", "Güncelleme sırasında bir hata oluştu."));
            }
        }
    }
}
