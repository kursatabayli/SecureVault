using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers;
using SecureVault.Vault.Application.Messages;
using SecureVault.Vault.Domain.Entities;
using System.Text;

namespace SecureVault.Vault.Application.Tests.Features.CQRS.VaultItems.Handlers
{
    public class UpdateVaultItemHandlerTests
    {
        private readonly Mock<IVaultItemsRepository> _repositoryMock;
        private readonly Mock<ILogger<UpdateVaultItemHandler>> _loggerMock;
        private readonly Mock<IStringLocalizer<ReturnMessages>> _localizerMock;
        private readonly UpdateVaultItemHandler _handler;

        public UpdateVaultItemHandlerTests()
        {
            _repositoryMock = new Mock<IVaultItemsRepository>();
            _loggerMock = new Mock<ILogger<UpdateVaultItemHandler>>();
            _localizerMock = new Mock<IStringLocalizer<ReturnMessages>>();

            _localizerMock.Setup(l => l[ErrorCodes.Vault.ItemNotFound]).Returns(new LocalizedString(ErrorCodes.Vault.ItemNotFound, "Item not found."));
            _localizerMock.Setup(l => l[ErrorCodes.UnauthorizedAccess]).Returns(new LocalizedString(ErrorCodes.UnauthorizedAccess, "Unauthorized access."));

            _handler = new UpdateVaultItemHandler(_repositoryMock.Object, _loggerMock.Object, _localizerMock.Object);
        }

        [Fact]
        public async Task Handle_Should_UpdateVaultItem_WhenItemExistsAndUserIsOwner()
        {
            var userId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var newData = Encoding.UTF8.GetBytes("new-data");
            var command = new UpdateVaultItemCommand(itemId, userId, newData);

            var vaultItem = (VaultItem)Activator.CreateInstance(typeof(VaultItem), true)!;
            typeof(VaultItem).GetProperty(nameof(VaultItem.UserId))!.SetValue(vaultItem, userId);
            typeof(VaultItem).GetProperty(nameof(VaultItem.Id))!.SetValue(vaultItem, itemId);
            typeof(VaultItem).GetProperty(nameof(VaultItem.Version))!.SetValue(vaultItem, 1);

            _repositoryMock.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(vaultItem);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            vaultItem.EncryptedData.Should().BeEquivalentTo(newData);
            vaultItem.Version.Should().Be(2);

            _repositoryMock.Verify(r => r.UpdateAsync(vaultItem), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnItemNotFound_WhenVaultItemDoesNotExist()
        {
            var command = new UpdateVaultItemCommand(Guid.NewGuid(), Guid.NewGuid(), []);
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((VaultItem?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.Vault.ItemNotFound);
        }

        [Fact]
        public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotOwner()
        {
            var ownerUserId = Guid.NewGuid();
            var attackerUserId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var command = new UpdateVaultItemCommand(itemId, attackerUserId, []);

            var vaultItem = (VaultItem)Activator.CreateInstance(typeof(VaultItem), true)!;
            typeof(VaultItem).GetProperty(nameof(VaultItem.UserId))!.SetValue(vaultItem, ownerUserId);
            typeof(VaultItem).GetProperty(nameof(VaultItem.Id))!.SetValue(vaultItem, itemId);

            _repositoryMock.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(vaultItem);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.UnauthorizedAccess);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<VaultItem>()), Times.Never);
        }
    }

}
