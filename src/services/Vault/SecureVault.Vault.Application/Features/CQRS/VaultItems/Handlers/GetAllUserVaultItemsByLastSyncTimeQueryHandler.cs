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
    internal class GetAllUserVaultItemsByLastSyncTimeQueryHandler : IRequestHandler<GetAllUserVaultItemsByLastSyncTimeQuery, Result<IReadOnlyCollection<GetAllUserVaultItemsByLastSyncTimeResult>>>
    {
        private readonly IVaultItemsRepository _repository;
        private readonly ILogger<GetAllUserVaultItemsByLastSyncTimeQueryHandler> _logger;
        private readonly IStringLocalizer<ReturnMessages> _returnMessages;

        public GetAllUserVaultItemsByLastSyncTimeQueryHandler(IVaultItemsRepository repository, ILogger<GetAllUserVaultItemsByLastSyncTimeQueryHandler> logger, IStringLocalizer<ReturnMessages> returnMessages)
        {
            _repository = repository;
            _logger = logger;
            _returnMessages = returnMessages;
        }

        public async Task<Result<IReadOnlyCollection<GetAllUserVaultItemsByLastSyncTimeResult>>> Handle(GetAllUserVaultItemsByLastSyncTimeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var spec = new VaultItemsByLastSyncTimeSpecification(request.UserId, request.LastUpdatedTime, request.DeviceId);

                var vaultItemsResult = await _repository.FindAsync(spec);

                return Result<IReadOnlyCollection<GetAllUserVaultItemsByLastSyncTimeResult>>.Success(vaultItemsResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcının vault item'ları listelenirken beklenmedik bir hata oluştu. UserId: {UserId}", request.UserId);
                return new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]);
            }
        }
    }
}
