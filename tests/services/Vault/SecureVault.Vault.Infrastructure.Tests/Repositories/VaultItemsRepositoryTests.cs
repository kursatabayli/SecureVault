using FluentAssertions;
using Mongo2Go;
using MongoDB.Driver;
using SecureVault.Vault.Domain.Entities;
using SecureVault.Vault.Domain.Enums;
using SecureVault.Vault.Infrastructure.Repositories;

namespace SecureVault.Vault.Infrastructure.Tests.Repositories
{
    public class VaultItemsRepositoryTests : IDisposable
    {
        private readonly MongoDbRunner _mongoDbRunner;
        private readonly IMongoDatabase _database;
        private readonly VaultItemsRepository _repository;

        public VaultItemsRepositoryTests()
        {
            _mongoDbRunner = MongoDbRunner.Start();

            var client = new MongoClient(_mongoDbRunner.ConnectionString);
            _database = client.GetDatabase("test_db");
            _repository = new VaultItemsRepository(_database);
        }

        private VaultItem CreateTestItem(Guid userId, ItemType type, bool isDeleted = false)
        {
            var item = VaultItem.Create(userId, type, []);
            if (isDeleted)
            {
                item.Delete();
            }
            return item;
        }

        [Fact]
        public async Task AddAsync_Should_InsertVaultItemIntoCollection()
        {
            var item = CreateTestItem(Guid.NewGuid(), ItemType.Password);

            await _repository.AddAsync(item);

            var itemInDb = await _database.GetCollection<VaultItem>("vaultItems")
                                          .Find(i => i.Id == item.Id)
                                          .SingleOrDefaultAsync();

            itemInDb.Should().NotBeNull();
            itemInDb.Should().BeEquivalentTo(item, options =>
                options.Using<DateTime>(ctx =>
                    ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(100)))
                    .WhenTypeIs<DateTime>());
        }

        [Fact]
        public async Task GetByIdAsync_Should_ReturnItem_WhenItExists()
        {
            var item = CreateTestItem(Guid.NewGuid(), ItemType.Password);
            await _repository.AddAsync(item);

            var result = await _repository.GetByIdAsync(item.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(item.Id);
        }

        [Fact]
        public async Task UpdateAsync_Should_ReplaceExistingItem()
        {
            var item = CreateTestItem(Guid.NewGuid(), ItemType.Password);
            await _repository.AddAsync(item);

            var newData = new byte[] { 1, 2, 3 };
            item.UpdateData(newData);

            var success = await _repository.UpdateAsync(item);

            success.Should().BeTrue();
            var updatedItemInDb = await _repository.GetByIdAsync(item.Id);
            updatedItemInDb!.EncryptedData.Should().BeEquivalentTo(newData);
            updatedItemInDb.Version.Should().Be(2);
        }

        [Fact]
        public async Task GetAllUserVaultItemsByVaultTypeAsync_Should_ReturnCorrectItems()
        {
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();

            var user1Password1 = CreateTestItem(userId1, ItemType.Password);
            var user1Password2 = CreateTestItem(userId1, ItemType.Password);
            var user1CreditCard = CreateTestItem(userId1, ItemType.CreditCard);
            var user1DeletedPassword = CreateTestItem(userId1, ItemType.Password, isDeleted: true);
            var user2Password = CreateTestItem(userId2, ItemType.Password);

            await _repository.AddAsync(user1Password1);
            await _repository.AddAsync(user1Password2);
            await _repository.AddAsync(user1CreditCard);
            await _repository.AddAsync(user1DeletedPassword);
            await _repository.AddAsync(user2Password);

            var result = await _repository.GetAllUserVaultItemsByVaultTypeAsync(userId1, ItemType.Password);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain(i => i.Id == user1Password1.Id);
            result.Should().Contain(i => i.Id == user1Password2.Id);
            result.Should().NotContain(i => i.Id == user1CreditCard.Id);
            result.Should().NotContain(i => i.Id == user1DeletedPassword.Id);
            result.Should().NotContain(i => i.Id == user2Password.Id);
        }

        public void Dispose() => _mongoDbRunner.Dispose();
    }

}
