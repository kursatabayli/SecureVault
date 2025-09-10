using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Queries;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Results;
using SecureVault.Vault.Application.Features.Specifications.VaultItems;
using SecureVault.Vault.Application.Messages;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers
{
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

                return Result<IReadOnlyCollection<VaultItemResult>>.Success(vaultItemsResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcının vault item'ları listelenirken beklenmedik bir hata oluştu. UserId: {UserId}, ItemType: {ItemType}", request.UserId, request.ItemType);
                return new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]);
            }
        }
    }
}
