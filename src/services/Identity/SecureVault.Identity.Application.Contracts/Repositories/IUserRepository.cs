using SecureVault.Identity.Domain.Entities;

namespace SecureVault.Identity.Application.Contracts.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetUserWithUserInfoAsync(Guid id);
        Task AddAsync(User user);
    }
}
