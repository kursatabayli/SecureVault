using FluentAssertions;
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
    public class RefreshTokenHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<RefreshTokenHandler>> _loggerMock;
        private readonly Mock<ITokenValidationService> _tokenValidationServiceMock;
        private readonly RefreshTokenHandler _handler;

        public RefreshTokenHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _authServiceMock = new Mock<IAuthService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<RefreshTokenHandler>>();
            _tokenValidationServiceMock = new Mock<ITokenValidationService>();

            _handler = new RefreshTokenHandler(
                _authServiceMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _userRepositoryMock.Object,
                Mock.Of<IStringLocalizer<ReturnMessages>>(),
                _tokenValidationServiceMock.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnNewTokens_WhenValidationSucceeds()
        {
            var command = new RefreshTokenCommand("access_token", "refresh_token", "127.0.0.1", "device-id", "TestDevice");
            var userId = Guid.NewGuid();
            var session = (UserSession)Activator.CreateInstance(typeof(UserSession), true)!;
            typeof(UserSession).GetProperty(nameof(UserSession.UserId))!.SetValue(session, userId);

            var user = (User)Activator.CreateInstance(typeof(User), true)!;
            typeof(User).GetProperty(nameof(User.UserInfo))!.SetValue(user, new UserInfo());

            var validationResult = Result<UserSession>.Success(session);

            _tokenValidationServiceMock.Setup(s => s.ValidateAndGetSessionAsync(command.AccessToken, command.RefreshToken, command.UniqueDeviceId))
                                       .ReturnsAsync(validationResult);

            _userRepositoryMock.Setup(r => r.GetUserWithUserInfoAsync(userId)).ReturnsAsync(user);

            _authServiceMock.Setup(a => a.GenerateJwtTokenForUser(user))
                            .Returns(("new_access_token", "new_access_jti", DateTime.UtcNow.AddMinutes(15)));

            _authServiceMock.Setup(a => a.GenerateRefreshTokenJwt(userId, session.IsPersistent))
                            .Returns(("new_refresh_token", "new_refresh_jti", DateTime.UtcNow.AddDays(30)));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.AccessToken.Should().Be("new_access_token");
            result.Value.RefreshToken.Should().Be("new_refresh_token");

            session.RefreshTokenJti.Should().Be("new_refresh_jti");
            session.AccessTokenJti.Should().Be("new_access_jti");

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }

}
