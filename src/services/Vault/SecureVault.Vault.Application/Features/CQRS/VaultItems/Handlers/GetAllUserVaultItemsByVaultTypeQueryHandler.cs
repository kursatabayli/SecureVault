using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.RepositoryContracts;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Queries;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Results;
using SecureVault.Vault.Application.Messages;
using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers
{
    public class GetAllUserVaultItemsByVaultTypeQueryHandler : IRequestHandler<GetAllUserVaultItemsByVaultTypeQuery, Result<IReadOnlyCollection<VaultItemResult>>>
    {
        private readonly IVaultItemsRepository _repository;
        private readonly ILogger<GetAllUserVaultItemsByVaultTypeQueryHandler> _logger;
        private readonly IStringLocalizer<ReturnMessages> _returnMessages;
        private readonly IMapper _mapper;

        public GetAllUserVaultItemsByVaultTypeQueryHandler(IVaultItemsRepository repository, ILogger<GetAllUserVaultItemsByVaultTypeQueryHandler> logger, IStringLocalizer<ReturnMessages> returnMessages, IMapper mapper)
        {
            _repository = repository;
            _logger = logger;
            _returnMessages = returnMessages;
            _mapper = mapper;
        }

        public async Task<Result<IReadOnlyCollection<VaultItemResult>>> Handle(GetAllUserVaultItemsByVaultTypeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var vaultItems = _repository.GetAllUserVaultItemsByVaultTypeAsync(request.UserId, request.ItemType);

                var mappedVaultItems = await _mapper.ProjectTo<VaultItemResult>(vaultItems).ToListAsync(cancellationToken);
                
                return mappedVaultItems;
            }
            catch (Exception ex)
            {
                string itemType = request.ItemType switch
                {
                    ItemType.Password => _returnMessages[ReturnMessages.ItemType_Password_Plural],
                    ItemType.TwoFactorAuth => _returnMessages[ReturnMessages.ItemType_TwoFactorAuth_Plural],
                    ItemType.CreditCard => _returnMessages[ReturnMessages.ItemType_CreditCard_Plural],
                    _ => throw new NotImplementedException(),
                };

                _logger.LogError(ex, _returnMessages[ReturnMessages.Error_Operation_List, itemType], nameof(ErrorCode.VaultListFailure));
                Error error = new(nameof(ErrorCode.VaultListFailure), _returnMessages[ReturnMessages.Error_Operation_List, itemType]);
                return error;
            }
        }
    }
}
