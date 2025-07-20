using Microsoft.EntityFrameworkCore;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Identity.Infrastructure.Context;

namespace SecureVault.Identity.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<User> _dbSet;
        public UserRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<User>();
        }

        public async Task AddAsync(User user) => await _dbSet.AddAsync(user);

        public async Task<User?> GetByEmailAsync(string email) => await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

        public async Task<User?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

        public Task<User?> GetUserWithUserInfoAsync(Guid id)
            => _dbSet.Include(u => u.UserInfo).FirstOrDefaultAsync(u => u.Id == id);
    }
}
