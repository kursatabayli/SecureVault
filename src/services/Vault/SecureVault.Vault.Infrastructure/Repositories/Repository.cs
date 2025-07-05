using Microsoft.EntityFrameworkCore;
using SecureVault.Vault.Application.Contracts.RepositoryContracts;
using SecureVault.Vault.Infrastructure.Context;
using System.Linq.Expressions;

namespace SecureVault.Vault.Infrastructure.Repositories
{
    public class Repository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }

        public IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> filter = null)
            => filter != null ? _dbSet.Where(filter) : _dbSet;

        public async Task<TEntity> GetByIdAsync(TKey id) => await _dbSet.FindAsync(id);

        public async Task CreateAsync(TEntity entity) => await _dbSet.AddAsync(entity);

        public void Update(TEntity entity) => _dbSet.Attach(entity);

        public void Delete(TEntity entity) => _dbSet.Remove(entity);
        public async Task DeleteByIdAsync(TKey id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
                _dbSet.Remove(entity);
        }

        public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> filter)
            => await _dbSet.AnyAsync(filter);

        public async Task<TEntity> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter)
            => await _dbSet.AsNoTracking().FirstOrDefaultAsync(filter);

    }
}
