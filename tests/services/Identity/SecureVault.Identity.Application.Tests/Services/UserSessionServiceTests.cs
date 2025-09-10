using FluentAssertions;
using Moq;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Application.Services;
using SecureVault.Identity.Domain.Entities;

namespace SecureVault.Identity.Application.Tests.Services
{
    public class UserSessionServiceTests
    {
        private readonly Mock<IUserSessionRepository> _userSessionRepositoryMock;
        private readonly UserSessionService _service;

        public UserSessionServiceTests()
        {
            _userSessionRepositoryMock = new Mock<IUserSessionRepository>();
            _service = new UserSessionService(_userSessionRepositoryMock.Object);
        }

        [Fact]
        public async Task ManageSessionAsync_Should_CreateNewSession_WhenNoExistingSessionForDevice()
        {
            var user = (User)Activator.CreateInstance(typeof(User), true)!;
            typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, Guid.NewGuid());
            var command = new LoginUserCommand("email", "sig", true, "new-device-id", null, null, null, null, null);

            _userSessionRepositoryMock.Setup(r => r.IsDeviceExistAsync(user.Id, command.UniqueDeviceId!))
                                      .ReturnsAsync((UserSession?)null);

            await _service.ManageSessionAsync(user, command, "access_jti", "refresh_jti", DateTime.UtcNow.AddDays(1));

            _userSessionRepositoryMock.Verify(r => r.AddAsync(It.Is<UserSession>(s => s.UserId == user.Id)), Times.Once);
        }

        [Fact]
        public async Task ManageSessionAsync_Should_UpdateExistingSession_WhenSessionForDeviceExists()
        {
            var user = (User)Activator.CreateInstance(typeof(User), true)!;
            typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, Guid.NewGuid());
            var command = new LoginUserCommand("email", "sig", true, "existing-device-id", null, null, null, null, null);
            var existingSession = (UserSession)Activator.CreateInstance(typeof(UserSession), true)!;

            _userSessionRepositoryMock.Setup(r => r.IsDeviceExistAsync(user.Id, command.UniqueDeviceId!))
                                      .ReturnsAsync(existingSession);

            await _service.ManageSessionAsync(user, command, "new_access_jti", "new_refresh_jti", DateTime.UtcNow.AddDays(1));

            _userSessionRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserSession>()), Times.Never);
            existingSession.RefreshTokenJti.Should().Be("new_refresh_jti");
        }
    }

}
