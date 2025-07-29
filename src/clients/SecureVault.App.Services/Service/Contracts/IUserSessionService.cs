using SecureVault.App.Services.Models.SessionModels;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services.Service.Contracts
{
    public interface IUserSessionService
    {
        Task<Result<List<UserSessionsModel>>> GetUserSessionsAsync();
        Task<Result> LogoutAnyWhereAsync(Guid sessionId);

    }
}
