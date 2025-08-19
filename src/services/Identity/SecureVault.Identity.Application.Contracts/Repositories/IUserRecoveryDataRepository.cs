using SecureVault.Identity.Domain.Entities;

namespace SecureVault.Identity.Application.Contracts.Repositories
{
    public interface IUserRecoveryDataRepository
    {
        Task<UserRecoveryData?> GetByUserIdAsync(Guid userId);
        Task CreateAsync(UserRecoveryData userRecoveryData);
    }
}
