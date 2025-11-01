using MediatR;
using SecureVault.Shared.Result;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;

public record CreateVaultItemListCommand(IEnumerable<CreateVaultItemCommand> VaultItems) : IRequest<Result>;
