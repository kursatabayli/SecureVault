using System.Linq.Expressions;

namespace SecureVault.App.Application.Contracts.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id);
        Task<List<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        Task<IReadOnlyList<T>> Find(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken);
    }
}
