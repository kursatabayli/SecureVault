using MediatR;
using SecureVault.Shared.Result;
using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands
{
    public record CreateVaultItemCommand(Guid Id, Guid UserId, ItemType ItemType, byte[] EncryptedData, DateTimeOffset CreatedAt, string LastUpdatedByDeviceId)
        : IRequest<Result>;
}
