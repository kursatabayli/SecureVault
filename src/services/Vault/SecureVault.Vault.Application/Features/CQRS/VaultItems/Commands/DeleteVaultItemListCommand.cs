using MediatR;
using SecureVault.Shared.Result;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;

public record DeleteVaultItemListCommand(IEnumerable<Guid> ItemIds, Guid UserId, string LastUpdatedByDeviceId) : IRequest<Result>;
