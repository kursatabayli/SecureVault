using MediatR;
using SecureVault.Shared.Result;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;

public record DeleteVaultItemCommand(Guid Id, Guid UserId, string LastUpdatedByDeviceId) : IRequest<Result>;
