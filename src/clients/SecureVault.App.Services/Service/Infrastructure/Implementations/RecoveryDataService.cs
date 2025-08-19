using Refit;
using SecureVault.App.Services.APIs;
using SecureVault.App.Services.Models.RecoveryKeyModels;
using SecureVault.App.Services.Service.Infrastructure.Contracts;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services.Service.Infrastructure.Implementations
{
    public class RecoveryDataService : IRecoveryDataService
    {
        private readonly ISecureVaultApi _secureVaultApi;

        public RecoveryDataService(ISecureVaultApi secureVaultApi)
        {
            _secureVaultApi = secureVaultApi;
        }
    }
}
