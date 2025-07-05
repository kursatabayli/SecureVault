using MediatR;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Results;
using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Queries
{
    public class GetAllUserVaultItemsByVaultTypeQuery : IRequest<Result<IReadOnlyCollection<VaultItemResult>>>
    {
        public ItemType ItemType { get; init; }
        public Guid UserId { get; init; }

        public GetAllUserVaultItemsByVaultTypeQuery(ItemType itemType, Guid userId)
        {
            ItemType = itemType;
            UserId = userId;
        }
    }
}
