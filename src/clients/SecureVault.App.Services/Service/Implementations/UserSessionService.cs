using SecureVault.App.Services.Constants;
using SecureVault.App.Services.Models.SessionModels;
using SecureVault.App.Services.Service.Contracts;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services.Service.Implementations
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IApiClient _apiClient;

        public UserSessionService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<Result<List<UserSessionsModel>>> GetUserSessionsAsync()
        {

            var apiResult = await _apiClient.GetAsync<List<UserSessionsModel>>(Endpoints.UserSessionBaseUrl, ClientTypes.AuthenticatedClient);
            if (apiResult.IsFailure)
                return apiResult.Error;

            return apiResult.Value;
        }

        public async Task<Result> LogoutAnyWhereAsync(Guid sessionId)
        {
            var apiResult = await _apiClient.DeleteAsync(Endpoints.UserSessionBaseUrl + sessionId, ClientTypes.AuthenticatedClient);
            if (apiResult.IsFailure)
                return apiResult;

            return apiResult;
        }
    }
}
