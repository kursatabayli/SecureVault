using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Contracts.Abstractions.Api
{
    public interface IInteractionService
    {
        Task<Result<string>> CreateQrLoginChannelAsync(CancellationToken cancellationToken);
    }
}
