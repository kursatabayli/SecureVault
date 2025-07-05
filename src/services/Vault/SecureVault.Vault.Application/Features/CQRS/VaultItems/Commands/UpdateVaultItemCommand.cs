using MediatR;
using SecureVault.Shared.Result;
using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands
{
    public record UpdateVaultItemCommand(Guid Id, Guid UserId, ItemType ItemType, byte[] EncryptedData)
        : IRequest<Result>;
}
