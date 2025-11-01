using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Messages;
using SecureVault.Vault.Domain.Entities;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers;

public class CreateVaultItemListHandler : IRequestHandler<CreateVaultItemListCommand, Result>
{
  private readonly IVaultItemsRepository _repository;
  private readonly ILogger<CreateVaultItemListHandler> _logger;
  private readonly IStringLocalizer<ReturnMessages> _returnMessages;
  public CreateVaultItemListHandler(IVaultItemsRepository repository, ILogger<CreateVaultItemListHandler> logger, IStringLocalizer<ReturnMessages> returnMessages)
  {
    _repository = repository;
    _logger = logger;
    _returnMessages = returnMessages;
  }

  public async Task<Result> Handle(CreateVaultItemListCommand request, CancellationToken cancellationToken)
  {
    if (request.VaultItems == null || !request.VaultItems.Any())
    {
      _logger.LogWarning("CreateVaultItemListCommand boş bir liste ile çağrıldı.");
      return Result.Success();
    }
    var firstItem = request.VaultItems.First();
    var userId = firstItem.UserId;
    var deviceId = firstItem.LastUpdatedByDeviceId;
    try
    {
      var vaultItems = request.VaultItems.Select(item => VaultItem.Create(
        item.Id,
        item.UserId,
        item.ItemType,
        item.EncryptedData,
        DateTimeOffset.UtcNow,
        item.LastUpdatedByDeviceId
      )).ToList();

      await _repository.AddRangeAsync(vaultItems);

      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Vault item oluşturulurken beklenmedik bir hata oluştu. UserId: {UserId}", request.VaultItems.FirstOrDefault().UserId);
      return Result.Failure(new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]));
    }
  }
}