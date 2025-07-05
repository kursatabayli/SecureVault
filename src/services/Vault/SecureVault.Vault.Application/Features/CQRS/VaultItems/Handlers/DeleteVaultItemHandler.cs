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
                if (vaultItem == null)
                {
                    Error error = new(nameof(ErrorCode.VaultItemNotFound), _returnMessages[ReturnMessages.Error_ItemNotFound]);
                    _logger.LogWarning(error.Message);
                    return Result.Failure(error);
                }

                if (vaultItem.UserId != request.UserId)
                {
                    Error error = new(nameof(ErrorCode.AuthorizationFailure), _returnMessages[ReturnMessages.Error_Unauthorized]);
                    _logger.LogWarning(error.Message);
                    return Result.Failure(error);
                }

                vaultItem.Delete();
                await _unitOfWork.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                Error error = new(nameof(ErrorCode.VaultDeleteFailure), _returnMessages[ReturnMessages.Error_Operation_Delete]);
                _logger.LogError(ex, error.Message);
                return Result.Failure(error);
            }
        }
    }
}
