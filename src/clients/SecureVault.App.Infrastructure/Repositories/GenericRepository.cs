using Realms;
using SecureVault.App.Application.Contracts.Abstractions.Database;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;
using System.Linq.Expressions;

namespace SecureVault.App.Infrastructure.Repositories;

public class GenericRepository<T> : IRepository<T> where T : class, IRealmObject, ISynchronizableEntity, new()
{
    private readonly IRealmService _realmService;

    public GenericRepository(IRealmService realmService)
    {
        _realmService = realmService;
    }

    public async Task<IList<T>> GetAllAsync()
    {
        var realm = await _realmService.GetRealmAsync();
        return realm.All<T>().ToList();
    }
    public async Task<T?> GetByIdAsync(Guid id)
    {
        var realm = await _realmService.GetRealmAsync();
        return realm.Find<T>(id);
    }

    public async Task<IRealmCollection<T>> GetLiveCollectionAsync()
    {
        var realm = await _realmService.GetRealmAsync();
        return realm.All<T>().AsRealmCollection();
    }
    public async Task AddAsync(T entity)
    {
        var realm = await _realmService.GetRealmAsync();
        await realm.WriteAsync(() =>
        {
            realm.Add(entity);
        });
    }
    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        var realm = await _realmService.GetRealmAsync();
        await realm.WriteAsync(() =>
        {
            foreach (var entity in entities)
            {
                realm.Add(entity);
            }
        });
    }
    public async Task UpdateAsync(T entity)
    {
        var realm = await _realmService.GetRealmAsync();
        await realm.WriteAsync(() =>
        {
            realm.Add(entity, update: true);
        });
    }

    public async Task UpdateRangeAsync(IEnumerable<T> entities)
    {
        var realm = await _realmService.GetRealmAsync();
        await realm.WriteAsync(() =>
        {
            foreach (var entity in entities)
            {
                realm.Add(entity, update: true);
            }
        });
    }

    public async Task MarkAsDeletedAsync(T entity)
    {
        var realm = await _realmService.GetRealmAsync();
        if (entity is ISynchronizableEntity synchronizableEntity)
        {
            await realm.WriteAsync(() =>
            {
                synchronizableEntity.MarkAsDeleted();
            });
        }
    }

    public async Task MarkAsDeletedRangeAsync(IEnumerable<T> entities)
    {
        var realm = await _realmService.GetRealmAsync();
        await realm.WriteAsync(() =>
        {
            foreach (var entity in entities)
            {
                if (entity is ISynchronizableEntity synchronizableEntity)
                {
                    synchronizableEntity.MarkAsDeleted();
                }
            }
        });
    }

    public async Task HardDeleteAsync(T entity)
    {
        var realm = await _realmService.GetRealmAsync();
        await realm.WriteAsync(() =>
        {
            realm.Remove(entity);
        });
    }

    public async Task HardDeleteRangeAsync(IEnumerable<T> entities)
    {
        var realm = await _realmService.GetRealmAsync();
        var idsToDelete = entities.Select(i => i.Id).ToHashSet();
        var allItems = realm.All<T>().ToList();
        var managedItemsToDelete = allItems.Where(p => idsToDelete.Contains(p.Id));
        await realm.WriteAsync(() =>
        {
            foreach (var item in managedItemsToDelete)
            {
                if (item != null && item.IsValid)
                {
                    realm.Remove(item);
                }
            }
        });
    }

    public async Task<IList<T>> Find(Expression<Func<T, bool>> predicate)
    {
        var realm = await _realmService.GetRealmAsync();
        return realm.All<T>().Where(predicate).ToList();
    }
    public async Task ApplyServerPullUpsertsAsync(IEnumerable<T> itemsToUpsert)
    {
        var realm = await _realmService.GetRealmAsync();
        await realm.WriteAsync(() =>
        {
            foreach (var item in itemsToUpsert)
            {
                realm.Add(item, update: true);
            }
        });
    }
    public async Task ApplyLocalPushChangesAsync(
        IEnumerable<T> itemsToMarkAsSynced,
        IEnumerable<T> itemsToHardDelete)
    {
        var realm = await _realmService.GetRealmAsync();
        await realm.WriteAsync(() =>
        {
            foreach (var item in itemsToMarkAsSynced)
            {
                if (item is ISynchronizableEntity syncable && item.IsValid)
                {
                    syncable.MarkAsSynced();
                }
            }

            foreach (var item in itemsToHardDelete)
            {
                if (item.IsValid)
                {
                    realm.Remove(item);
                }
            }
        });
    }
}
