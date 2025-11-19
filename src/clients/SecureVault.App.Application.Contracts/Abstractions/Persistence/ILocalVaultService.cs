using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Contracts.Abstractions.Persistence;

public interface ILocalVaultService
{
    Task<Result> SetAllVaultDataAsync(CancellationToken cancellationToken);
}
