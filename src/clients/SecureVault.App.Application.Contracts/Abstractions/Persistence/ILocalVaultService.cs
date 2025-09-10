using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Contracts.Abstractions.Cryptography
{
    public interface ILocalVaultService
    {
        Task<Result> SetAllVaultDataAsync(CancellationToken cancellationToken);
    }
}
