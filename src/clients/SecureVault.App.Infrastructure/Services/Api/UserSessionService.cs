using Refit;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.DTOs.Session;
using SecureVault.Shared.Result;
using System.Threading;

namespace SecureVault.App.Infrastructure.Services.Api
{
    public class UserSessionService : IUserSessionService
    {
        private readonly ISecureVaultAuthorizeApi _secureVaultApi;

        public UserSessionService(ISecureVaultAuthorizeApi secureVaultApi)
        {
            _secureVaultApi = secureVaultApi;
        }

        public async Task<Result<List<UserSessionsDto>>> GetUserSessionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var sessions = await _secureVaultApi.GetUserSessionsAsync(cancellationToken);
                return sessions;
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result<List<UserSessionsDto>>.Failure(error ?? new Error("Client.LoadFailed", "Oturumlar yüklenemedi."));
            }
        }

        public async Task<Result> LogoutAnyWhereAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _secureVaultApi.LogoutSessionAsync(sessionId, cancellationToken);

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
