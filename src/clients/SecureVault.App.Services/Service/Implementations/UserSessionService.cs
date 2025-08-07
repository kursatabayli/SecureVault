using Refit;
using SecureVault.App.Services.APIs;
using SecureVault.App.Services.Models.SessionModels;
using SecureVault.App.Services.Service.Contracts;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services.Service.Implementations
{
    public class UserSessionService : IUserSessionService
    {
        private readonly ISecureVaultApi _secureVaultApi;

        public UserSessionService(ISecureVaultApi secureVaultApi)
        {
            _secureVaultApi = secureVaultApi;
        }

        public async Task<Result<List<UserSessionsModel>>> GetUserSessionsAsync()
        {
            try
            {
                var sessions = await _secureVaultApi.GetUserSessionsAsync();
                return sessions;
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result<List<UserSessionsModel>>.Failure(error ?? new Error("Client.LoadFailed", "Oturumlar yüklenemedi."));
            }
        }

        public async Task<Result> LogoutAnyWhereAsync(Guid sessionId)
        {
            try
            {
                var response = await _secureVaultApi.LogoutSessionAsync(sessionId);

                return response.IsSuccessStatusCode
                    ? Result.Success()
                    : Result.Failure(await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.DeleteFailed", "Oturum sonlandırılamadı."));
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.DeleteFailed", "Oturum sonlandırılamadı."));
            }
        }
    }
}
