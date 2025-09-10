using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Commands;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Handlers;
using SecureVault.Identity.Application.Messages;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Tests.Features.CQRS.UserSessions.Handlers
{
    public class RevokeSessionHandlerTests
    {
        private readonly Mock<IUserSessionRepository> _sessionRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly RevokeSessionHandler _handler;

        public RevokeSessionHandlerTests()
        {
            _sessionRepositoryMock = new Mock<IUserSessionRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _cacheServiceMock = new Mock<ICacheService>();
            _handler = new RevokeSessionHandler(
                _sessionRepositoryMock.Object,
                _unitOfWorkMock.Object,
                Mock.Of<ILogger<RevokeSessionHandler>>(),
                Mock.Of<IStringLocalizer<ReturnMessages>>(),
                _cacheServiceMock.Object);
        }

        [Fact]
        public async Task Handle_Should_RevokeSessionAndBlacklistToken_WhenSessionIsValid()
        {
            var currentUserId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();
            var command = new RevokeSessionCommand(sessionId, currentUserId);

            var sessionToRevoke = (UserSession)Activator.CreateInstance(typeof(UserSession), true)!;
            typeof(UserSession).GetProperty(nameof(UserSession.UserId))!.SetValue(sessionToRevoke, currentUserId);
            typeof(UserSession).GetProperty(nameof(UserSession.AccessTokenJti))!.SetValue(sessionToRevoke, "access_jti_to_blacklist");
            typeof(UserSession).GetProperty(nameof(UserSession.LastUsedAt))!.SetValue(sessionToRevoke, DateTimeOffset.UtcNow.AddMinutes(-5));

            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(sessionToRevoke);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            sessionToRevoke.IsRevoked.Should().BeTrue();
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
            _cacheServiceMock.Verify(c => c.SetAsync(
                $"blacklist:{sessionToRevoke.AccessTokenJti}",
                "revoked",
                It.Is<TimeSpan>(ts => ts > TimeSpan.Zero)), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotOwner()
        {
            var ownerUserId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();
            var command = new RevokeSessionCommand(sessionId, currentUserId);

            var sessionToRevoke = (UserSession)Activator.CreateInstance(typeof(UserSession), true)!;
            typeof(UserSession).GetProperty(nameof(UserSession.UserId))!.SetValue(sessionToRevoke, ownerUserId);

            _sessionRepositoryMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(sessionToRevoke);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ErrorCodes.UnauthorizedAccess);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }
    }

}
