using MongoDB.Driver;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Domain.Entities;
using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Infrastructure.Repositories
{
    public class VaultItemsRepository : IVaultItemsRepository
    {
        private readonly IMongoCollection<VaultItem> _collection;

        // MongoDbContext'i veya doğrudan IMongoDatabase'i inject edebilirsiniz.
        public VaultItemsRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<VaultItem>("vaultItems");
        }

        public async Task AddAsync(VaultItem vaultItem) => await _collection.InsertOneAsync(vaultItem);

        public async Task<IReadOnlyCollection<VaultItem>> GetAllUserVaultItemsByVaultTypeAsync(Guid userId, ItemType itemType)
        {
            var filter = Builders<VaultItem>.Filter.And(
                Builders<VaultItem>.Filter.Eq(v => v.UserId, userId),
                Builders<VaultItem>.Filter.Eq(v => v.ItemType, itemType),
                Builders<VaultItem>.Filter.Eq(v => v.IsDeleted, false)
            );
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<VaultItem?> GetByIdAsync(Guid id) => await _collection.Find(v => v.Id == id).SingleOrDefaultAsync();

        public async Task<bool> UpdateAsync(VaultItem vaultItem)
        {
            var filter = Builders<VaultItem>.Filter.Eq(v => v.Id, vaultItem.Id);
            var result = await _collection.ReplaceOneAsync(filter, vaultItem);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

    }
}
