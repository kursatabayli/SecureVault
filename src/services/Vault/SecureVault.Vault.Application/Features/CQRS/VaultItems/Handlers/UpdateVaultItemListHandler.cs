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
      _logger.LogWarning("UpdateVaultItemListCommand boş bir liste ile çağrıldı.");
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
        _logger.LogWarning("Var olmayan bir vault item güncellenmeye çalışıldı. ItemId: {ItemId}", firstItem.Id);
        return Result.Failure(new Error(ErrorCodes.Vault.ItemNotFound, _returnMessages[ErrorCodes.Vault.ItemNotFound]));
      }

      if (vaultItemsFromDb.Any(v => v.UserId != userId))
      {
        _logger.LogCritical("YETKİSİZ ERİŞİM DENEMESİ: Kullanıcı {UserId}, kendisine ait olmayan bir item'ı ({ItemId}) güncellemeye çalıştı.", userId, firstItem.Id);
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
          _logger.LogWarning("Güncelleme listesindeki item (Id: {ItemId}) veritabanında bulunamadı. Senkronizasyon uyuşmazlığı nedeniyle atlanıyor. UserId: {UserId}", itemToUpdate.Id, userId);
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
      _logger.LogError(ex, "Vault item güncellenirken beklenmedik bir hata oluştu. ItemId: {ItemId}", firstItem.Id);
      return Result.Failure(new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]));
    }
  }
}
