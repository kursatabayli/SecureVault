using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Handlers;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Queries;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Results;
using SecureVault.Vault.Application.Messages;
using SecureVault.Vault.Domain.Entities;
using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Application.Tests.Features.CQRS.VaultItems.Handlers
{

    public class GetAllUserVaultItemsByVaultTypeQueryHandlerTests
    {
        private readonly Mock<IVaultItemsRepository> _repositoryMock;
        private readonly Mock<ILogger<GetAllUserVaultItemsByVaultTypeQueryHandler>> _loggerMock;
        private readonly Mock<IStringLocalizer<ReturnMessages>> _localizerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly GetAllUserVaultItemsByVaultTypeQueryHandler _handler;

        public GetAllUserVaultItemsByVaultTypeQueryHandlerTests()
        {
            _repositoryMock = new Mock<IVaultItemsRepository>();
            _loggerMock = new Mock<ILogger<GetAllUserVaultItemsByVaultTypeQueryHandler>>();
            _localizerMock = new Mock<IStringLocalizer<ReturnMessages>>();
            _mapperMock = new Mock<IMapper>();

            _handler = new GetAllUserVaultItemsByVaultTypeQueryHandler(
                _repositoryMock.Object,
                _loggerMock.Object,
                _localizerMock.Object,
                _mapperMock.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnMappedVaultItems_WhenItemsExist()
        {
            var query = new GetAllUserVaultItemsByVaultTypeQuery(ItemType.Password, Guid.NewGuid());
            var vaultItemsFromRepo = new List<VaultItem>
        {
            (VaultItem)Activator.CreateInstance(typeof(VaultItem), true)!,
            (VaultItem)Activator.CreateInstance(typeof(VaultItem), true)!
        };

            var mappedResults = new List<VaultItemResult>
        {
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() }
        };

            _repositoryMock.Setup(r => r.GetAllUserVaultItemsByVaultTypeAsync(query.UserId, query.ItemType))
                           .ReturnsAsync(vaultItemsFromRepo);

            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<VaultItemResult>>(vaultItemsFromRepo))
                       .Returns(mappedResults);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(mappedResults);
            _repositoryMock.Verify(r => r.GetAllUserVaultItemsByVaultTypeAsync(query.UserId, query.ItemType), Times.Once);
            _mapperMock.Verify(m => m.Map<IReadOnlyCollection<VaultItemResult>>(vaultItemsFromRepo), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmptyList_WhenNoItemsExist()
        {
            var query = new GetAllUserVaultItemsByVaultTypeQuery(ItemType.Password, Guid.NewGuid());
            var emptyVaultItems = new List<VaultItem>().AsReadOnly();
            var emptyMappedResults = new List<VaultItemResult>().AsReadOnly();

            _repositoryMock.Setup(r => r.GetAllUserVaultItemsByVaultTypeAsync(query.UserId, query.ItemType))
                           .ReturnsAsync(emptyVaultItems);

            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<VaultItemResult>>(emptyVaultItems))
                       .Returns(emptyMappedResults);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEmpty();
        }
    }

}
