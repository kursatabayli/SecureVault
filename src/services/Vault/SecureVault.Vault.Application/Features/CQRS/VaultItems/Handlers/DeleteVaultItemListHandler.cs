using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Messages;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers;

public class DeleteVaultItemListHandler : IRequestHandler<DeleteVaultItemListCommand, Result>
{
  private readonly IVaultItemsRepository _repository;
  private readonly ILogger<DeleteVaultItemListHandler> _logger;
  private readonly IStringLocalizer<ReturnMessages> _returnMessages;
  public DeleteVaultItemListHandler(IVaultItemsRepository repository, ILogger<DeleteVaultItemListHandler> logger, IStringLocalizer<ReturnMessages> returnMessages)
  {
    _repository = repository;
    _logger = logger;
    _returnMessages = returnMessages;
  }

  public async Task<Result> Handle(DeleteVaultItemListCommand request, CancellationToken cancellationToken)
  {
    if (request.ItemIds == null || !request.ItemIds.Any())
    {
      _logger.LogWarning("DeleteVaultItemListCommand was called with an empty list.");
      return Result.Success();
    }
    var firstItem = request.ItemIds.First();
    try
    {
      var vaultItemsFromDb = await _repository.GetByIdsAsync(request.ItemIds);

      if (!vaultItemsFromDb.Any())
      {
        _logger.LogWarning("None of the items requested for deletion were found in the database. UserId: {UserId}", request.UserId);
        return Result.Success();
      }

      if (vaultItemsFromDb.Any(item => item.UserId != request.UserId))
      {
        _logger.LogCritical("UNAUTHORIZED ACCESS ATTEMPT: User {UserId} attempted to delete an item ({ItemId}) that does not belong to them.", request.UserId, firstItem);
        return Result.Failure(new Error(ErrorCodes.UnauthorizedAccess, _returnMessages[ErrorCodes.UnauthorizedAccess]));
      }
      var dbItemsDict = vaultItemsFromDb.ToDictionary(k => k.Id);
      foreach (var requestedId in request.ItemIds)
      {
        if (dbItemsDict.TryGetValue(requestedId, out var vaultItem))
        {
          vaultItem.Delete(request.LastUpdatedByDeviceId);
        }
        else
        {
          _logger.LogWarning("Item from delete list (Id: {ItemId}) not found in database. Skipping due to potential synchronization mismatch. UserId: {UserId}", requestedId, request.UserId);
        }
      }

      await _repository.UpdateRangeAsync(vaultItemsFromDb);

      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An unexpected error occurred while deleting vault items. First ItemId from batch: {ItemId}", firstItem);
      return Result.Failure(new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]));
    }
  }
}
