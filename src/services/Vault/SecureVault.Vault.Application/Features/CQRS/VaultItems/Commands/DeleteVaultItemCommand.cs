using MediatR;
using SecureVault.Shared.Result;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands
{
    public class DeleteVaultItemCommand : IRequest<Result>
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public DeleteVaultItemCommand(Guid id, Guid userId)
        {
            Id = id;
            UserId = userId;
        }
    }
}
