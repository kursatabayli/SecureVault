using SecureVault.App.Application.Contracts.DTOs.Session;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Contracts.Abstractions.Api;

public interface IUserSessionService
{
    Task<Result<List<UserSessionsDto>>> GetUserSessionsAsync(CancellationToken cancellationToken);
    Task<Result> LogoutAnyWhereAsync(Guid sessionId, CancellationToken cancellationToken);

}
