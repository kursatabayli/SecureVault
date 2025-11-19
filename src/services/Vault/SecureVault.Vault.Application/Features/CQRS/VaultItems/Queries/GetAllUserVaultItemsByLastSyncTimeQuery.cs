using MediatR;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Results;

namespace SecureVault.Vault.Application.Features.CQRS.VaultItems.Queries;

public record GetAllUserVaultItemsByLastSyncTimeQuery(Guid UserId, DateTimeOffset LastUpdatedTime, string DeviceId) : IRequest<Result<IReadOnlyCollection<GetAllUserVaultItemsByLastSyncTimeResult>>>;
