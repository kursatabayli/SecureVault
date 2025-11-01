using MongoDB.Driver;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Application.Contracts.Specifications;
using SecureVault.Vault.Domain.Entities;

namespace SecureVault.Vault.Infrastructure.Repositories
{
    public class VaultItemsRepository : IVaultItemsRepository
    {
        private readonly IMongoCollection<VaultItem> _collection;

        public VaultItemsRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<VaultItem>("vaultItems");
        }

        public async Task AddAsync(VaultItem vaultItem) => await _collection.InsertOneAsync(vaultItem);
        public async Task AddRangeAsync(IEnumerable<VaultItem> vaultItems) => await _collection.InsertManyAsync(vaultItems);

        public async Task<IReadOnlyCollection<TResult>> FindAsync<TResult>(ISpecification<VaultItem, TResult> spec)
            => await _collection.Find(spec.Criteria)
                                .Project(spec.Projection)
                                .ToListAsync();

        public async Task<VaultItem?> GetByIdAsync(Guid id) => await _collection.Find(v => v.Id == id).SingleOrDefaultAsync();

        public async Task<IEnumerable<VaultItem>> GetByIdsAsync(IEnumerable<Guid> ids) => await _collection.Find(v => ids.Contains(v.Id)).ToListAsync();
        public async Task<DateTimeOffset?> GetLatestUpdateTimeAsync(Guid userId)
        {
            var latestDate = await _collection.Find(v => v.UserId == userId)
                                            .SortByDescending(v => v.UpdatedAt)
                                            .Limit(1)
                                            .Project(v => v.UpdatedAt)
                                            .SingleOrDefaultAsync();

            if (latestDate == default)
                return null;

            return latestDate;
        }

        public async Task<bool> UpdateAsync(VaultItem vaultItem)
        {
            var filter = Builders<VaultItem>.Filter.Eq(v => v.Id, vaultItem.Id);
            var result = await _collection.ReplaceOneAsync(filter, vaultItem);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateRangeAsync(IEnumerable<VaultItem> vaultItems)
        {
            var itemList = vaultItems.ToList();
            if (!itemList.Any())
                return true;

            var models = new List<WriteModel<VaultItem>>();

            foreach (var item in itemList)
            {
                var filter = Builders<VaultItem>.Filter.Eq(v => v.Id, item.Id);
                var replaceOne = new ReplaceOneModel<VaultItem>(filter, item);
                models.Add(replaceOne);
            }

            var result = await _collection.BulkWriteAsync(models);
            return result.IsAcknowledged && result.MatchedCount == itemList.Count;
        }

    }
}
