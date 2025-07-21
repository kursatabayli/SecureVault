using MediatR;
using SecureVault.Shared.Result;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands
{
    public record UpdateVaultItemCommand(Guid Id, Guid UserId, byte[] EncryptedData)
        : IRequest<Result>;
}
