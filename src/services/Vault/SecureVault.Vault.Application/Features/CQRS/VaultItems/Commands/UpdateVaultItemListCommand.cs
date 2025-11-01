using MediatR;
using SecureVault.Shared.Result;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;

public record UpdateVaultItemListCommand(IEnumerable<UpdateVaultItemCommand> VaultItems) : IRequest<Result>;
