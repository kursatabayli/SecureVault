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
    public class VerifyLoginSignatureHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<IEcdsaVerificationService> _ecdsaServiceMock;
        private readonly Mock<IStringLocalizer<ReturnMessages>> _localizerMock;
        private readonly Mock<ILogger<VerifyLoginSignatureHandler>> _loggerMock;
        private readonly VerifyLoginSignatureHandler _handler;

        public VerifyLoginSignatureHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _cacheServiceMock = new Mock<ICacheService>();
            _ecdsaServiceMock = new Mock<IEcdsaVerificationService>();
            _localizerMock = new Mock<IStringLocalizer<ReturnMessages>>();
            _loggerMock = new Mock<ILogger<VerifyLoginSignatureHandler>>();

            _handler = new VerifyLoginSignatureHandler(
                _userRepositoryMock.Object,
                _cacheServiceMock.Object,
                _ecdsaServiceMock.Object,
                _localizerMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnUser_WhenSignatureIsValid()
        {
            var command = new VerifyLoginSignatureCommand("test@example.com", "valid_signature");
            var user = (User)Activator.CreateInstance(typeof(User), true)!;
            typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, Guid.NewGuid());
            var challenge = "random_challenge_string";
            var cacheKey = $"login_challenge:{user.Id}";

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email)).ReturnsAsync(user);
            _cacheServiceMock.Setup(c => c.GetAsync<string>(cacheKey)).ReturnsAsync(challenge);
            _ecdsaServiceMock.Setup(s => s.VerifySignature(challenge, command.Signature, It.IsAny<byte[]>())).Returns(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(user);

            _cacheServiceMock.Verify(c => c.RemoveAsync(cacheKey), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnLoginFailed_WhenUserDoesNotExist()
        {
            var command = new VerifyLoginSignatureCommand("nonexistent@example.com", "any_signature");
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email)).ReturnsAsync((User?)null);
            _localizerMock.Setup(_ => _[ErrorCodes.Auth.LoginFailed]).Returns(new LocalizedString(ErrorCodes.Auth.LoginFailed, "Login failed."));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.Auth.LoginFailed);
        }

        [Fact]
        public async Task Handle_Should_ReturnChallengeExpired_WhenChallengeIsNotInCache()
        {
            var command = new VerifyLoginSignatureCommand("test@example.com", "any_signature");
            var user = (User)Activator.CreateInstance(typeof(User), true)!;
            typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, Guid.NewGuid());
            var cacheKey = $"login_challenge:{user.Id}";

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email)).ReturnsAsync(user);
            _cacheServiceMock.Setup(c => c.GetAsync<string>(cacheKey)).ReturnsAsync((string?)null);
            _localizerMock.Setup(_ => _[ErrorCodes.Auth.ChallengeExpired]).Returns(new LocalizedString(ErrorCodes.Auth.ChallengeExpired, "Challenge expired."));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.Auth.ChallengeExpired);
        }

        [Fact]
        public async Task Handle_Should_ReturnLoginFailed_WhenSignatureIsInvalid()
        {
            var command = new VerifyLoginSignatureCommand("test@example.com", "invalid_signature");
            var user = (User)Activator.CreateInstance(typeof(User), true)!;
            typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, Guid.NewGuid());
            var challenge = "random_challenge_string";
            var cacheKey = $"login_challenge:{user.Id}";

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email)).ReturnsAsync(user);
            _cacheServiceMock.Setup(c => c.GetAsync<string>(cacheKey)).ReturnsAsync(challenge);
            _ecdsaServiceMock.Setup(s => s.VerifySignature(challenge, command.Signature, It.IsAny<byte[]>())).Returns(false);
            _localizerMock.Setup(_ => _[ErrorCodes.Auth.LoginFailed]).Returns(new LocalizedString(ErrorCodes.Auth.LoginFailed, "Login failed."));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.Auth.LoginFailed);

            _cacheServiceMock.Verify(c => c.RemoveAsync(cacheKey), Times.Never);
        }
    }

}
