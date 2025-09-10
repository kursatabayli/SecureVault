using SecureVault.App.Application.Contracts.Abstractions.Api;

namespace SecureVault.App.Infrastructure.Services.Api
{
    public class InteractionService : IInteractionService
    {
        private readonly ISecureVaultAnonymousApi _secureVaultAnonymousApi;

        public InteractionService(ISecureVaultAnonymousApi secureVaultAnonymousApi)
        {
            _secureVaultAnonymousApi = secureVaultAnonymousApi;
        }

        public async Task<string> CreateQrLoginChannelAsync(CancellationToken cancellationToken)
        {
            var response = await _secureVaultAnonymousApi.CreateChannelAsync(cancellationToken);
            return response.ChannelId;
        }
    }
}
