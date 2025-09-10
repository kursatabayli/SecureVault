using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Application.Features.CQRS.Register.Handlers;
using SecureVault.Identity.Application.Messages;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Tests.Features.CQRS.Auth.Handlers
{
    public class RegisterUserHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IStringLocalizer<ReturnMessages>> _localizerMock;
        private readonly Mock<ILogger<RegisterUserHandler>> _loggerMock;
        private readonly RegisterUserHandler _handler;

        public RegisterUserHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _localizerMock = new Mock<IStringLocalizer<ReturnMessages>>();
            _loggerMock = new Mock<ILogger<RegisterUserHandler>>();

            _handler = new RegisterUserHandler(
                _userRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _localizerMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_WhenEmailIsNotInUse()
        {
            var command = new RegisterUserCommand(
                Guid.NewGuid(),
                "newuser@example.com",
                [1],
                [2],
                new UserInfo { Name = "New", Surname = "User" }
            );

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email))
                               .ReturnsAsync((User?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == command.Email)), Times.Once);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmailInUseError_WhenEmailExists()
        {
            var command = new RegisterUserCommand(
                Guid.NewGuid(),
                "existing@example.com",
                [1],
                [2],
                new UserInfo { Name = "Existing", Surname = "User" }
            );

            var existingUser = (User)Activator.CreateInstance(typeof(User), true)!;
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email))
                               .ReturnsAsync(existingUser);

            var localizedString = new LocalizedString(ErrorCodes.Auth.EmailInUse, "Email is already in use.");
            _localizerMock.Setup(_ => _[ErrorCodes.Auth.EmailInUse]).Returns(localizedString);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.Auth.EmailInUse);
            result.Error.Message.Should().Be("Email is already in use.");

            _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }
    }

}
