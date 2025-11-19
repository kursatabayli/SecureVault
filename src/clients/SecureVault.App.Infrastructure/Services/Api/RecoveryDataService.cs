using SecureVault.App.Application.Contracts.Abstractions.Api;

namespace SecureVault.App.Infrastructure.Services.Api;

public class RecoveryDataService : IRecoveryDataService
{
    private readonly ISecureVaultAuthorizeApi _secureVaultApi;

    public RecoveryDataService(ISecureVaultAuthorizeApi secureVaultApi)
    {
        _secureVaultApi = secureVaultApi;
    }
}
