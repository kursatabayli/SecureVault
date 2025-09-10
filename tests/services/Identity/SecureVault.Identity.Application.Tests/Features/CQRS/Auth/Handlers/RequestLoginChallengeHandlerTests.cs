using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Application.Features.CQRS.Auth.Handlers;
using SecureVault.Identity.Application.Messages;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Tests.Features.CQRS.Auth.Handlers
{

    public class RequestLoginChallengeHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<IStringLocalizer<ReturnMessages>> _localizerMock;
        private readonly Mock<ILogger<RequestLoginChallengeHandler>> _loggerMock;
        private readonly RequestLoginChallengeHandler _handler;

        public RequestLoginChallengeHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _cacheServiceMock = new Mock<ICacheService>();
            _localizerMock = new Mock<IStringLocalizer<ReturnMessages>>();
            _loggerMock = new Mock<ILogger<RequestLoginChallengeHandler>>();

            _handler = new RequestLoginChallengeHandler(
                _userRepositoryMock.Object,
                _cacheServiceMock.Object,
                _localizerMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnChallenge_WhenUserExists()
        {
            // Arrange
            var command = new RequestLoginChallengeCommand("test@example.com");
            var userSalt = new byte[] { 1, 2, 3, 4 };
            var user = (User)Activator.CreateInstance(typeof(User), true)!;
            typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, Guid.NewGuid());
            typeof(User).GetProperty(nameof(User.Salt))!.SetValue(user, userSalt);

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email))
                               .ReturnsAsync(user);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Challenge.Should().NotBeNullOrWhiteSpace();
            result.Value.Salt.Should().BeEquivalentTo(userSalt);

            var expectedCacheKey = $"login_challenge:{user.Id}";
            _cacheServiceMock.Verify(c => c.SetAsync(
                expectedCacheKey,
                It.IsAny<string>(),
                TimeSpan.FromSeconds(60)
            ), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnUserNotFound_WhenUserDoesNotExist()
        {
            var command = new RequestLoginChallengeCommand("nonexistent@example.com");

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email))
                               .ReturnsAsync((User?)null);

            var localizedString = new LocalizedString(ErrorCodes.Auth.UserNotFound, "User not found.");
            _localizerMock.Setup(_ => _[ErrorCodes.Auth.UserNotFound]).Returns(localizedString);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.Auth.UserNotFound);
            result.Error.Message.Should().Be("User not found.");

            _cacheServiceMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnInternalServerError_WhenRepositoryThrowsException()
        {
            var command = new RequestLoginChallengeCommand("test@example.com");
            var exception = new Exception("Database error");

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email))
                               .ThrowsAsync(exception);

            var localizedString = new LocalizedString(ErrorCodes.InternalServerError, "An unexpected error occurred.");
            _localizerMock.Setup(_ => _[ErrorCodes.InternalServerError]).Returns(localizedString);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.InternalServerError);
            result.Error.Message.Should().Be("An unexpected error occurred.");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Login challenge isteği sırasında beklenmedik bir hata oluştu.")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }
    }

}
