using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Application.Features.CQRS.Auth.Handlers;
using SecureVault.Identity.Application.Messages;
using SecureVault.Identity.Application.Services;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Tests.Features.CQRS.Auth.Handlers
{
    public class LoginUserHandlerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IUserSessionService> _userSessionServiceMock;
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<LoginUserHandler>> _loggerMock;
        private readonly Mock<IStringLocalizer<ReturnMessages>> _localizerMock;
        private readonly LoginUserHandler _handler;

        public LoginUserHandlerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _userSessionServiceMock = new Mock<IUserSessionService>();
            _authServiceMock = new Mock<IAuthService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<LoginUserHandler>>();
            _localizerMock = new Mock<IStringLocalizer<ReturnMessages>>();

            _handler = new LoginUserHandler(
                _mediatorMock.Object,
                _userRepositoryMock.Object,
                _authServiceMock.Object,
                _unitOfWorkMock.Object,
                _localizerMock.Object,
                _loggerMock.Object,
                _userSessionServiceMock.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnAuthResponse_WhenVerificationIsSuccessful()
        {
            var command = new LoginUserCommand("test@example.com", "valid_signature", true, "device-id", "TestDevice", "ModelX", "Manu", "OS", "127.0.0.1");

            var user = (User)Activator.CreateInstance(typeof(User), true)!;
            typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, Guid.NewGuid());
            typeof(User).GetProperty(nameof(User.UserInfo))!.SetValue(user, new UserInfo());

            var successfulVerificationResult = Result<User>.Success(user);

            _mediatorMock.Setup(m => m.Send(It.IsAny<VerifyLoginSignatureCommand>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(successfulVerificationResult);

            _userRepositoryMock.Setup(r => r.GetUserWithUserInfoAsync(user.Id)).ReturnsAsync(user);

            _authServiceMock.Setup(a => a.GenerateJwtTokenForUser(user))
                            .Returns(("access_token", "access_jti", DateTime.UtcNow.AddMinutes(15)));

            _authServiceMock.Setup(a => a.GenerateRefreshTokenJwt(user.Id, command.RememberMe))
                            .Returns(("refresh_token", "refresh_jti", DateTime.UtcNow.AddDays(30)));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.AccessToken.Should().Be("access_token");
            result.Value.RefreshToken.Should().Be("refresh_token");

            _userSessionServiceMock.Verify(s => s.ManageSessionAsync(user, command, "access_jti", "refresh_jti", It.IsAny<DateTime>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_WhenVerificationFails()
        {
            var command = new LoginUserCommand("test@example.com", "invalid_signature", false, null, null, null, null, null, null);
            var verificationError = new Error("Auth.LoginFailed", "Invalid signature");
            var failedVerificationResult = Result<User>.Failure(verificationError);

            _mediatorMock.Setup(m => m.Send(It.IsAny<VerifyLoginSignatureCommand>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(failedVerificationResult);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(verificationError);

            _authServiceMock.Verify(a => a.GenerateJwtTokenForUser(It.IsAny<User>()), Times.Never);
            _userSessionServiceMock.Verify(s => s.ManageSessionAsync(It.IsAny<User>(), It.IsAny<LoginUserCommand>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }
    }

}
