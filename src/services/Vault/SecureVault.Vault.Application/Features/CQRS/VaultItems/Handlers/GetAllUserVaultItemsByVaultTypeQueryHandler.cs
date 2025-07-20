using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.Repositories;
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
                
                return Result<IReadOnlyCollection<VaultItemResult>>.Success(mappedVaultItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcının vault item'ları listelenirken beklenmedik bir hata oluştu. UserId: {UserId}, ItemType: {ItemType}", request.UserId, request.ItemType);
                return new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]);
            }
        }
    }
}
