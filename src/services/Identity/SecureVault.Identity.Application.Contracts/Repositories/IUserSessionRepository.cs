using SecureVault.Identity.Domain.Entities;

namespace SecureVault.Identity.Application.Contracts.Repositories
{
    public interface IUserSessionRepository
    {
        Task AddAsync(UserSession userSession);
        Task<IReadOnlyCollection<UserSession>> GetAllSessionsByUserIdAsync(Guid userId);
        Task<UserSession?> GetByIdAsync(Guid sessionId);
        Task<UserSession?> GetSessionByJtiAsync(string jti);
        Task<UserSession?> IsDeviceExistAsync(Guid userId, string uniqueDeviceId);
    }
}
