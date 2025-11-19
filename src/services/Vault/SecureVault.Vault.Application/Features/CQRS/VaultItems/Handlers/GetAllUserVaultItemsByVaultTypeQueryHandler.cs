using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Queries;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Results;
using SecureVault.Vault.Application.Features.Specifications.VaultItems;
using SecureVault.Vault.Application.Messages;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers;

public class GetAllUserVaultItemsByVaultTypeQueryHandler : IRequestHandler<GetAllUserVaultItemsByVaultTypeQuery, Result<IReadOnlyCollection<VaultItemResult>>>
{
    private readonly IVaultItemsRepository _repository;
    private readonly ILogger<GetAllUserVaultItemsByVaultTypeQueryHandler> _logger;
    private readonly IStringLocalizer<ReturnMessages> _returnMessages;

    public GetAllUserVaultItemsByVaultTypeQueryHandler(IVaultItemsRepository repository, ILogger<GetAllUserVaultItemsByVaultTypeQueryHandler> logger, IStringLocalizer<ReturnMessages> returnMessages)
    {
        _repository = repository;
        _logger = logger;
        _returnMessages = returnMessages;
    }

    public async Task<Result<IReadOnlyCollection<VaultItemResult>>> Handle(GetAllUserVaultItemsByVaultTypeQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var spec = new VaultItemsByUserIdAndItemTypeSpecification(request.UserId, request.ItemType);

            var vaultItemsResult = await _repository.FindAsync(spec);

            if (vaultItemsResult == null || !vaultItemsResult.Any())
                _logger.LogInformation("No vault items found for User: {UserId}, ItemType: {ItemType}.", request.UserId, request.ItemType);
            else
                _logger.LogInformation("Successfully retrieved {ItemCount} vault items for User: {UserId}, ItemType: {ItemType}.", vaultItemsResult.Count, request.UserId, request.ItemType);

            return Result<IReadOnlyCollection<VaultItemResult>>.Success(vaultItemsResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving vault items for user. UserId: {UserId}, ItemType: {ItemType}", request.UserId, request.ItemType);
            return new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]);
        }
    }
}