using System.Linq.Expressions;

namespace SecureVault.Vault.Application.Contracts.Repositories
{
    public interface IRepository<TEntity, TKey> where TEntity : class
    {
        IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> filter = null);
        Task<TEntity> GetByIdAsync(TKey id);
        Task CreateAsync(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        Task DeleteByIdAsync(TKey id);
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> filter);
        Task<TEntity> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter);
    }
}
