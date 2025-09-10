using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Messages;
using SecureVault.Identity.Application.Services;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;
using System.Security.Claims;

namespace SecureVault.Identity.Application.Tests.Services
{
    public class TokenValidationServiceTests
    {
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<IUserSessionRepository> _userSessionRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly TokenValidationService _service;

        public TokenValidationServiceTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _userSessionRepositoryMock = new Mock<IUserSessionRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _service = new TokenValidationService(
                _authServiceMock.Object,
                _userSessionRepositoryMock.Object,
                _unitOfWorkMock.Object,
                Mock.Of<IStringLocalizer<ReturnMessages>>(),
                Mock.Of<ILogger<TokenValidationService>>());
        }

        private ClaimsPrincipal CreatePrincipal(string userId, string jti)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(JwtRegisteredClaimNames.Jti, jti)
        }));
        }

        [Fact]
        public async Task ValidateAndGetSessionAsync_Should_ReturnSession_WhenTokensAreValidAndMatch()
        {
            var userId = Guid.NewGuid();
            var jti = "valid_jti";
            var session = (UserSession)Activator.CreateInstance(typeof(UserSession), true)!;
            typeof(UserSession).GetProperty(nameof(UserSession.UserId))!.SetValue(session, userId);

            var accessPrincipal = CreatePrincipal(userId.ToString(), "any_access_jti");
            var refreshPrincipal = CreatePrincipal(userId.ToString(), jti);

            _authServiceMock.Setup(a => a.GetPrincipalFromAccessToken("access_token", false)).Returns(accessPrincipal);
            _authServiceMock.Setup(a => a.GetPrincipalFromRefreshToken("refresh_token")).Returns(refreshPrincipal);
            _userSessionRepositoryMock.Setup(r => r.GetSessionByJtiAsync(jti)).ReturnsAsync(session);

            var result = await _service.ValidateAndGetSessionAsync("access_token", "refresh_token");

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(session);
        }

        [Fact]
        public async Task ValidateAndGetSessionAsync_Should_ReturnError_WhenSessionNotFound()
        {
            var userId = Guid.NewGuid();
            var jti = "non_existent_jti";
            var accessPrincipal = CreatePrincipal(userId.ToString(), "any_access_jti");
            var refreshPrincipal = CreatePrincipal(userId.ToString(), jti);

            _authServiceMock.Setup(a => a.GetPrincipalFromAccessToken(It.IsAny<string>(), It.IsAny<bool>())).Returns(accessPrincipal);
            _authServiceMock.Setup(a => a.GetPrincipalFromRefreshToken(It.IsAny<string>())).Returns(refreshPrincipal);
            _userSessionRepositoryMock.Setup(r => r.GetSessionByJtiAsync(jti)).ReturnsAsync((UserSession?)null);

            var result = await _service.ValidateAndGetSessionAsync("access_token", "refresh_token");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.Auth.InvalidRefreshToken);
        }

        [Fact]
        public async Task ValidateAndGetSessionAsync_Should_RevokeAndReturnError_WhenDeviceIdMismatch()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var jti = "valid_jti";
            var deviceDetails = new DeviceDetail { UniqueDeviceId = "device-1" };
            var session = (UserSession)Activator.CreateInstance(typeof(UserSession), true)!;
            typeof(UserSession).GetProperty(nameof(UserSession.UserId))!.SetValue(session, userId);
            typeof(UserSession).GetProperty(nameof(UserSession.DeviceDetails))!.SetValue(session, deviceDetails);

            var accessPrincipal = CreatePrincipal(userId.ToString(), "any_access_jti");
            var refreshPrincipal = CreatePrincipal(userId.ToString(), jti);

            _authServiceMock.Setup(a => a.GetPrincipalFromAccessToken(It.IsAny<string>(), It.IsAny<bool>())).Returns(accessPrincipal);
            _authServiceMock.Setup(a => a.GetPrincipalFromRefreshToken(It.IsAny<string>())).Returns(refreshPrincipal);
            _userSessionRepositoryMock.Setup(r => r.GetSessionByJtiAsync(jti)).ReturnsAsync(session);

            // Act
            var result = await _service.ValidateAndGetSessionAsync("access_token", "refresh_token", "different-device-id");

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.Auth.InvalidRefreshToken);
            session.IsRevoked.Should().BeTrue();
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }

}
