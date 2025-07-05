using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.RepositoryContracts;
using SecureVault.Vault.Application.Contracts.ServicesContracts;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Messages;
using SecureVault.Vault.Domain.Entities;
using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers
{
    public class CreateVaultItemHandler : IRequestHandler<CreateVaultItemCommand, Result>
    {
        private readonly IVaultItemsRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateVaultItemHandler> _logger;
        private readonly IStringLocalizer<ReturnMessages> _returnMessages;

        public CreateVaultItemHandler(IUnitOfWork unitOfWork, ILogger<CreateVaultItemHandler> logger, IStringLocalizer<ReturnMessages> returnMessages, IVaultItemsRepository repository)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _returnMessages = returnMessages;
            _repository = repository;
        }

        public async Task<Result> Handle(CreateVaultItemCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var vaultItem = VaultItem.Create(
                    request.UserId,
                    request.ItemType,
                    request.EncryptedData
                );

                await _repository.AddAsync(vaultItem);
                await _unitOfWork.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                string itemType = request.ItemType switch
                {
                    ItemType.Password => _returnMessages[ReturnMessages.ItemType_Password],
                    ItemType.TwoFactorAuth => _returnMessages[ReturnMessages.ItemType_TwoFactorAuth],
                    ItemType.CreditCard => _returnMessages[ReturnMessages.ItemType_CreditCard],
                    _ => throw new NotImplementedException(),
                };
                string errorMessage = _returnMessages[ReturnMessages.Error_Operation_Create, itemType];

                _logger.LogError(ex, errorMessage);
                var error = new Error(nameof(ErrorCode.VaultCreateFailure), errorMessage);
                return Result.Failure(error);

            }
        }
    }
}
