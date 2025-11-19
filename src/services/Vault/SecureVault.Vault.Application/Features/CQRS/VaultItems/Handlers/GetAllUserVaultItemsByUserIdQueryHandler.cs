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

public class GetAllUserVaultItemsByUserIdQueryHandler : IRequestHandler<GetAllUserVaultItemsByUserIdQuery, Result<IReadOnlyCollection<VaultItemResult>>>
{
    private readonly IVaultItemsRepository _repository;
    private readonly ILogger<GetAllUserVaultItemsByUserIdQueryHandler> _logger;
    private readonly IStringLocalizer<ReturnMessages> _returnMessages;

    public GetAllUserVaultItemsByUserIdQueryHandler(IVaultItemsRepository repository, ILogger<GetAllUserVaultItemsByUserIdQueryHandler> logger, IStringLocalizer<ReturnMessages> returnMessages)
    {
        _repository = repository;
        _logger = logger;
        _returnMessages = returnMessages;
    }

    public async Task<Result<IReadOnlyCollection<VaultItemResult>>> Handle(GetAllUserVaultItemsByUserIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var spec = new VaultItemsByUserIdSpecification(request.UserId);

            var vaultItemsResult = await _repository.FindAsync(spec);

            if (vaultItemsResult == null || !vaultItemsResult.Any())
            {
                _logger.LogInformation("No vault items found for User: {UserId}.", request.UserId);
            }
            else
            {
                _logger.LogInformation("Successfully retrieved {ItemCount} total vault items for User: {UserId}.", vaultItemsResult.Count, request.UserId);
            }

            return Result<IReadOnlyCollection<VaultItemResult>>.Success(vaultItemsResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving all vault items for user. UserId: {UserId}", request.UserId);
            return new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]);
        }
    }
}
