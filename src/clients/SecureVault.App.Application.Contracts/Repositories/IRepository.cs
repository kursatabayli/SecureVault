using Realms;
using SecureVault.App.Domain.Entities;
using System.Linq.Expressions;

namespace SecureVault.App.Application.Contracts.Repositories;

public interface IRepository<T> where T : IRealmObject, ISynchronizableEntity, new()
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IList<T>> GetAllAsync();
    Task<IRealmCollection<T>> GetLiveCollectionAsync();
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task UpdateRangeAsync(IEnumerable<T> entities);
    Task MarkAsDeletedAsync(T entity);
    Task MarkAsDeletedRangeAsync(IEnumerable<T> entities);
    Task HardDeleteAsync(T entity);
    Task HardDeleteRangeAsync(IEnumerable<T> entities);
    Task<IList<T>> Find(Expression<Func<T, bool>> predicate);

    Task ApplyServerPullUpsertsAsync(IEnumerable<T> itemsToUpsert);
    Task ApplyLocalPushChangesAsync(IEnumerable<T> itemsToMarkAsSynced, IEnumerable<T> itemsToHardDelete);
}
