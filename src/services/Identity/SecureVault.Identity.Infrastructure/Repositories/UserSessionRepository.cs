using Microsoft.EntityFrameworkCore;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Identity.Infrastructure.Context;

namespace SecureVault.Identity.Infrastructure.Repositories
{
    public class UserSessionRepository : IUserSessionRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<UserSession> _dbSet;
        public UserSessionRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<UserSession>();
        }

        public async Task AddAsync(UserSession userSession) => await _dbSet.AddAsync(userSession);

        public async Task<UserSession?> GetByIdAsync(Guid sessionId)
        {
            throw new NotImplementedException();
        }

        public async Task<UserSession?> GetSessionByJtiAsync(string jti)
            => await _dbSet.Where(session =>
                    session.TokenIdentifier == jti &&
                    !session.IsRevoked &&
                    session.ExpiresAt > DateTimeOffset.UtcNow)
                .Include(x => x.DeviceDetails)
                .FirstOrDefaultAsync();

        public async Task<IReadOnlyCollection<UserSession>> GetAllSessionsByUserIdAsync(Guid userId)
            => await _dbSet.Where(session => session.UserId == userId).Include(x => x.DeviceDetails).AsNoTracking().ToListAsync();

        public async Task<UserSession?> IsDeviceExistAsync(Guid userId, string uniqueDeviceId)
            => await _dbSet.Include(x => x.DeviceDetails)
                .Where(x => x.UserId == userId && x.DeviceDetails.UniqueDeviceId == uniqueDeviceId)
                .FirstOrDefaultAsync();
    }
}
