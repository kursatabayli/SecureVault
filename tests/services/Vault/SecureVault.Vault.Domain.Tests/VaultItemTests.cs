using SecureVault.Vault.Domain.Entities;
using SecureVault.Vault.Domain.Enums;
using System.Text;
using FluentAssertions;

namespace SecureVault.Vault.Domain.Tests
{
    public class VaultItemTests
    {
        [Fact]
        public void Create_Should_InitializeVaultItem_Correctly()
        {
            var userId = Guid.NewGuid();
            var itemType = ItemType.Password;
            var encryptedData = Encoding.UTF8.GetBytes("test-data");

            var vaultItem = VaultItem.Create(userId, itemType, encryptedData);

            vaultItem.Should().NotBeNull();
            vaultItem.Id.Should().NotBe(Guid.Empty);
            vaultItem.UserId.Should().Be(userId);
            vaultItem.ItemType.Should().Be(itemType);
            vaultItem.EncryptedData.Should().BeEquivalentTo(encryptedData);
            vaultItem.Version.Should().Be(1);
            vaultItem.IsDeleted.Should().BeFalse();
            vaultItem.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            vaultItem.UpdatedAt.Should().Be(vaultItem.CreatedAt);
        }

        [Fact]
        public void UpdateData_Should_UpdateEncryptedDataAndVersion_And_SetUpdatedAt()
        {
            var vaultItem = VaultItem.Create(Guid.NewGuid(), ItemType.Password, Encoding.UTF8.GetBytes("initial-data"));
            var initialVersion = vaultItem.Version;
            var initialUpdatedAt = vaultItem.UpdatedAt;
            var newEncryptedData = Encoding.UTF8.GetBytes("updated-data");

            Thread.Sleep(10);

            vaultItem.UpdateData(newEncryptedData);

            vaultItem.EncryptedData.Should().BeEquivalentTo(newEncryptedData);
            vaultItem.Version.Should().Be(initialVersion + 1);
            vaultItem.UpdatedAt.Should().BeAfter(initialUpdatedAt);
        }

        [Fact]
        public void Delete_Should_SetIsDeletedToTrue_And_SetUpdatedAt()
        {
            var vaultItem = VaultItem.Create(Guid.NewGuid(), ItemType.Password, Encoding.UTF8.GetBytes("some-data"));
            var initialUpdatedAt = vaultItem.UpdatedAt;

            Thread.Sleep(10);

            vaultItem.Delete();

            vaultItem.IsDeleted.Should().BeTrue();
            vaultItem.UpdatedAt.Should().BeAfter(initialUpdatedAt);
        }

        [Fact]
        public void UpdateData_OnDeletedItem_Should_ThrowInvalidOperationException()
        {
            var vaultItem = VaultItem.Create(Guid.NewGuid(), ItemType.Password, Encoding.UTF8.GetBytes("some-data"));
            vaultItem.Delete();
            var newEncryptedData = Encoding.UTF8.GetBytes("updated-data");

            Action act = () => vaultItem.UpdateData(newEncryptedData);

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Cannot update a deleted item.");
        }
    }
}
