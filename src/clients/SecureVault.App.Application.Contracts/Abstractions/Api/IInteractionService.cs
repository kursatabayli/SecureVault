namespace SecureVault.App.Application.Contracts.Abstractions.Api
{
    public interface IInteractionService
    {
        Task<string> CreateQrLoginChannelAsync(CancellationToken cancellationToken);
    }
}
