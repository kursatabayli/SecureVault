using Microsoft.EntityFrameworkCore;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Identity.Infrastructure.Context;

namespace SecureVault.Identity.Infrastructure.Repositories;

public class UserRecoveryDataRepository : IUserRecoveryDataRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<UserRecoveryData> _dbSet;
    public UserRecoveryDataRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<UserRecoveryData>();
    }
    public async Task CreateAsync(UserRecoveryData userRecoveryData) => await _dbSet.AddAsync(userRecoveryData);
    public async Task<UserRecoveryData?> GetByUserIdAsync(Guid userId) => await _dbSet.FindAsync(userId);
}
