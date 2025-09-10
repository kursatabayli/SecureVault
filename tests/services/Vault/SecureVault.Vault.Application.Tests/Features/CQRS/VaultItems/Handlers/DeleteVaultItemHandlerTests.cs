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

namespace SecureVault.Vault.Application.Tests.Features.CQRS.VaultItems.Handlers
{
    public class DeleteVaultItemHandlerTests
    {
        private readonly Mock<IVaultItemsRepository> _repositoryMock;
        private readonly Mock<ILogger<DeleteVaultItemHandler>> _loggerMock;
        private readonly Mock<IStringLocalizer<ReturnMessages>> _localizerMock;
        private readonly DeleteVaultItemHandler _handler;

        public DeleteVaultItemHandlerTests()
        {
            _repositoryMock = new Mock<IVaultItemsRepository>();
            _loggerMock = new Mock<ILogger<DeleteVaultItemHandler>>();
            _localizerMock = new Mock<IStringLocalizer<ReturnMessages>>();

            _localizerMock.Setup(l => l[ErrorCodes.Vault.ItemNotFound]).Returns(new LocalizedString(ErrorCodes.Vault.ItemNotFound, "Item not found."));
            _localizerMock.Setup(l => l[ErrorCodes.UnauthorizedAccess]).Returns(new LocalizedString(ErrorCodes.UnauthorizedAccess, "Unauthorized access."));

            _handler = new DeleteVaultItemHandler(_repositoryMock.Object, _loggerMock.Object, _localizerMock.Object);
        }

        [Fact]
        public async Task Handle_Should_DeleteVaultItem_WhenItemExistsAndUserIsOwner()
        {
            var userId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var command = new DeleteVaultItemCommand(itemId, userId);

            var vaultItem = (VaultItem)Activator.CreateInstance(typeof(VaultItem), true)!;
            typeof(VaultItem).GetProperty(nameof(VaultItem.UserId))!.SetValue(vaultItem, userId);
            typeof(VaultItem).GetProperty(nameof(VaultItem.Id))!.SetValue(vaultItem, itemId);

            _repositoryMock.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(vaultItem);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<VaultItem>())).ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            vaultItem.IsDeleted.Should().BeTrue();

            _repositoryMock.Verify(r => r.UpdateAsync(vaultItem), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnItemNotFound_WhenVaultItemDoesNotExist()
        {
            var command = new DeleteVaultItemCommand(Guid.NewGuid(), Guid.NewGuid());

            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((VaultItem?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.Vault.ItemNotFound);
            result.Error.Message.Should().Be("Item not found.");
        }

        [Fact]
        public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotOwner()
        {
            var ownerUserId = Guid.NewGuid();
            var attackerUserId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var command = new DeleteVaultItemCommand(itemId, attackerUserId);

            var vaultItem = (VaultItem)Activator.CreateInstance(typeof(VaultItem), true)!;
            typeof(VaultItem).GetProperty(nameof(VaultItem.Id))!.SetValue(vaultItem, itemId);

            _repositoryMock.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(vaultItem);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.UnauthorizedAccess);
            result.Error.Message.Should().Be("Unauthorized access.");

            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<VaultItem>()), Times.Never);
        }
    }

}
