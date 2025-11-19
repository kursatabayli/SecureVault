using MediatR;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Results;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Queries;

public class GetAllUserVaultItemsByUserIdQuery : IRequest<Result<IReadOnlyCollection<VaultItemResult>>>
{
    public Guid UserId { get; init; }

    public GetAllUserVaultItemsByUserIdQuery(Guid userId)
    {
        UserId = userId;
    }
}
