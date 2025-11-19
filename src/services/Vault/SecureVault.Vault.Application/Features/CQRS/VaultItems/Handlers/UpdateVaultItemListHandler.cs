using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Messages;
using SecureVault.Vault.Domain.Entities;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers;

public class UpdateVaultItemListHandler : IRequestHandler<UpdateVaultItemListCommand, Result>
{
    private readonly IVaultItemsRepository _repository;
    private readonly ILogger<UpdateVaultItemListHandler> _logger;
    private readonly IStringLocalizer<ReturnMessages> _returnMessages;
    public UpdateVaultItemListHandler(IVaultItemsRepository repository, ILogger<UpdateVaultItemListHandler> logger, IStringLocalizer<ReturnMessages> returnMessages)
    {
        _repository = repository;
        _logger = logger;
        _returnMessages = returnMessages;
    }

    public async Task<Result> Handle(UpdateVaultItemListCommand request, CancellationToken cancellationToken)
    {
        if (request.VaultItems == null || !request.VaultItems.Any())
        {
            _logger.LogWarning("UpdateVaultItemListCommand was called with an empty list.");
            return Result.Success();
        }
        var firstItem = request.VaultItems.First();
        var userId = firstItem.UserId;
        var deviceId = firstItem.LastUpdatedByDeviceId;
        try
        {
            var requestedIds = request.VaultItems.Select(v => v.Id).ToList();
            var vaultItemsFromDb = await _repository.GetByIdsAsync(requestedIds);

            if (vaultItemsFromDb is null)
            {
                _logger.LogWarning("Attempted to update a non-existent vault item. ItemId: {ItemId}", firstItem.Id);
                return Result.Failure(new Error(ErrorCodes.Vault.ItemNotFound, _returnMessages[ErrorCodes.Vault.ItemNotFound]));
            }

            if (vaultItemsFromDb.Any(v => v.UserId != userId))
            {
                _logger.LogCritical("UNAUTHORIZED ACCESS ATTEMPT: User {UserId} attempted to update an item ({ItemId}) that does not belong to them.", userId, firstItem.Id);
                return Result.Failure(new Error(ErrorCodes.UnauthorizedAccess, _returnMessages[ErrorCodes.UnauthorizedAccess]));
            }

            var dbItemsDict = vaultItemsFromDb.ToDictionary(k => k.Id);
            var itemsToUpdateInDb = new List<VaultItem>();
            foreach (var itemToUpdate in request.VaultItems)
            {
                if (dbItemsDict.TryGetValue(itemToUpdate.Id, out var vaultItem))
                {
                    vaultItem.UpdateData(itemToUpdate.EncryptedData, itemToUpdate.UpdatedAt, itemToUpdate.LastUpdatedByDeviceId);
                    itemsToUpdateInDb.Add(vaultItem);
                }
                else
                {
                    _logger.LogWarning("Item from update list (Id: {ItemId}) not found in database. Skipping due to potential synchronization mismatch. UserId: {UserId}", itemToUpdate.Id, userId);
                }
            }

            if (itemsToUpdateInDb.Any())
            {
                await _repository.UpdateRangeAsync(itemsToUpdateInDb);
            }
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while updating vault items. First ItemId from batch: {ItemId}", firstItem.Id);
            return Result.Failure(new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]));
        }
    }
}
