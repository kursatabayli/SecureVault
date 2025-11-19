using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Messages;
using SecureVault.Vault.Domain.Entities;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers;

public class CreateVaultItemHandler : IRequestHandler<CreateVaultItemCommand, Result>
{
    private readonly IVaultItemsRepository _repository;
    private readonly ILogger<CreateVaultItemHandler> _logger;
    private readonly IStringLocalizer<ReturnMessages> _returnMessages;
    public CreateVaultItemHandler(IVaultItemsRepository repository, ILogger<CreateVaultItemHandler> logger, IStringLocalizer<ReturnMessages> returnMessages)
    {
        _repository = repository;
        _logger = logger;
        _returnMessages = returnMessages;
    }

    public async Task<Result> Handle(CreateVaultItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var vaultItem = VaultItem.Create(
                request.Id,
                request.UserId,
                request.ItemType,
                request.EncryptedData,
                request.CreatedAt,
                request.LastUpdatedByDeviceId
            );

            await _repository.AddAsync(vaultItem);

            _logger.LogInformation(
                                "Vault item created successfully. ItemId: {ItemId}, UserId: {UserId}, CreatedByDevice: {DeviceId}",
                                vaultItem.Id, vaultItem.UserId, vaultItem.LastUpdatedByDeviceId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                                "An unexpected error occurred while creating the vault item. ItemId: {ItemId}, UserId: {UserId}, DeviceId: {DeviceId}",
                                request.Id, request.UserId, request.LastUpdatedByDeviceId);
            return Result.Failure(new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]));
        }
    }
}
