using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Messages;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers;

public class UpdateVaultItemHandler : IRequestHandler<UpdateVaultItemCommand, Result>
{
    private readonly IVaultItemsRepository _repository;
    private readonly ILogger<UpdateVaultItemHandler> _logger;
    private readonly IStringLocalizer<ReturnMessages> _returnMessages;
    public UpdateVaultItemHandler(IVaultItemsRepository repository, ILogger<UpdateVaultItemHandler> logger, IStringLocalizer<ReturnMessages> returnMessages)
    {
        _repository = repository;
        _logger = logger;
        _returnMessages = returnMessages;
    }

    public async Task<Result> Handle(UpdateVaultItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var vaultItem = await _repository.GetByIdAsync(request.Id);

            if (vaultItem is null)
            {
                _logger.LogWarning("Attempted to update a non-existent vault item. ItemId: {ItemId}", request.Id);
                return Result.Failure(new Error(ErrorCodes.Vault.ItemNotFound, _returnMessages[ErrorCodes.Vault.ItemNotFound]));
            }

            if (vaultItem.UserId != request.UserId)
            {
                _logger.LogCritical("UNAUTHORIZED ACCESS ATTEMPT: User {UserId} attempted to update an item ({ItemId}) that does not belong to them.", request.UserId, request.Id);
                return Result.Failure(new Error(ErrorCodes.UnauthorizedAccess, _returnMessages[ErrorCodes.UnauthorizedAccess]));
            }

            vaultItem.UpdateData(request.EncryptedData, request.UpdatedAt, request.LastUpdatedByDeviceId);
            await _repository.UpdateAsync(vaultItem);

            _logger.LogInformation(
                "Vault item updated successfully. ItemId: {ItemId}, UserId: {UserId}, UpdatedByDevice: {DeviceId}",
                vaultItem.Id, vaultItem.UserId, request.LastUpdatedByDeviceId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while updating the vault item. ItemId: {ItemId}", request.Id);
            return Result.Failure(new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]));
        }
    }
}