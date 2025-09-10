using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers;
using SecureVault.Vault.Application.Messages;
using SecureVault.Vault.Domain.Enums;
using System.Text;
using FluentAssertions;
using SecureVault.Vault.Domain.Entities;

namespace SecureVault.Vault.Application.Tests.Features.CQRS.VaultItems.Handlers
{
    public class CreateVaultItemHandlerTests
    {
        private readonly Mock<IVaultItemsRepository> _repositoryMock;
        private readonly Mock<ILogger<CreateVaultItemHandler>> _loggerMock;
        private readonly Mock<IStringLocalizer<ReturnMessages>> _localizerMock;
        private readonly CreateVaultItemHandler _handler;

        public CreateVaultItemHandlerTests()
        {
            _repositoryMock = new Mock<IVaultItemsRepository>();
            _loggerMock = new Mock<ILogger<CreateVaultItemHandler>>();
            _localizerMock = new Mock<IStringLocalizer<ReturnMessages>>();

            _handler = new CreateVaultItemHandler(_repositoryMock.Object, _loggerMock.Object, _localizerMock.Object);
        }

        [Fact]
        public async Task Handle_Should_CreateVaultItemAndReturnSuccess_WhenRequestIsValid()
        {
            var command = new CreateVaultItemCommand(
                Guid.NewGuid(),
                ItemType.Password,
                Encoding.UTF8.GetBytes("test-encrypted-data")
            );

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<VaultItem>()))
                           .Returns(Task.CompletedTask);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().Be(Error.None);

            _repositoryMock.Verify(r => r.AddAsync(It.Is<VaultItem>(item =>
                item.UserId == command.UserId &&
                item.ItemType == command.ItemType &&
                item.EncryptedData == command.EncryptedData
            )), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailureResult_WhenRepositoryThrowsException()
        {
            var command = new CreateVaultItemCommand(
                Guid.NewGuid(),
                ItemType.Password,
                Encoding.UTF8.GetBytes("test-data")
            );
            var exception = new Exception("Database connection failed");

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<VaultItem>()))
                           .ThrowsAsync(exception);

            var localizedString = new LocalizedString(ErrorCodes.InternalServerError, "Internal server error occurred.");
            _localizerMock.Setup(_ => _[ErrorCodes.InternalServerError]).Returns(localizedString);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.InternalServerError);
            result.Error.Message.Should().Be("Internal server error occurred.");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Vault item oluşturulurken beklenmedik bir hata oluştu.")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }
    }
}
