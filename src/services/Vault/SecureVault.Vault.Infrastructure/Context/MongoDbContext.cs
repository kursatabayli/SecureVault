using MongoDB.Driver;
using SecureVault.Vault.Domain.Entities;

namespace SecureVault.Vault.Infrastructure.Context;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    public MongoDbContext(IMongoDatabase database)
    {
        _database = database;
    }
    public IMongoCollection<VaultItem> VaultItems => _database.GetCollection<VaultItem>("vaultItems");
}
