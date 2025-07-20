using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Application.Contracts.Services;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Messages;
using SecureVault.Vault.Domain.Entities;

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
                _logger.LogError(ex, "Vault item oluşturulurken beklenmedik bir hata oluştu. UserId: {UserId}", request.UserId);
                return Result.Failure(new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]));
            }
        }
    }
}
