using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Identity.Infrastructure.Context;
using SecureVault.Identity.Infrastructure.Repositories;

namespace SecureVault.Identity.Infrastructure.Tests.Repositories
{
    public class UserSessionRepositoryTests
    {
        private readonly AppDbContext _context;
        private readonly UserSessionRepository _sessionRepository;

        public UserSessionRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _sessionRepository = new UserSessionRepository(_context);
        }

        private UserSession CreateTestSession(Guid userId, string jti, bool isRevoked = false, int expiresAfterDays = 7)
        {
            return UserSession.Create(
                userId,
                jti,
                "access_jti",
                new DeviceDetail { UniqueDeviceId = $"device-{jti}" },
                "127.0.0.1",
                DateTimeOffset.UtcNow.AddDays(expiresAfterDays),
                isRevoked,
                true
            );
        }

        [Fact]
        public async Task GetSessionByJtiAsync_Should_ReturnSession_WhenValidAndNotExpired()
        {
            var userId = Guid.NewGuid();
            var jti = "valid-jti";
            var session = CreateTestSession(userId, jti);

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            var result = await _sessionRepository.GetSessionByJtiAsync(jti);

            result.Should().NotBeNull();
            result!.Id.Should().Be(session.Id);
        }

        [Fact]
        public async Task GetSessionByJtiAsync_Should_ReturnNull_WhenRevoked()
        {
            var userId = Guid.NewGuid();
            var jti = "revoked-jti";
            var session = CreateTestSession(userId, jti, isRevoked: true);

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            var result = await _sessionRepository.GetSessionByJtiAsync(jti);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetSessionByJtiAsync_Should_ReturnNull_WhenExpired()
        {
            var userId = Guid.NewGuid();
            var jti = "expired-jti";
            var session = CreateTestSession(userId, jti, expiresAfterDays: -1);

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            var result = await _sessionRepository.GetSessionByJtiAsync(jti);

            result.Should().BeNull();
        }

        [Fact]
        public async Task IsDeviceExistAsync_Should_ReturnSession_WhenDeviceExistsForUser()
        {
            var userId = Guid.NewGuid();
            var deviceId = "existing-device";
            var session = CreateTestSession(userId, "any-jti");
            typeof(UserSession).GetProperty(nameof(UserSession.DeviceDetails))!.SetValue(session, new DeviceDetail { UniqueDeviceId = deviceId });

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            var result = await _sessionRepository.IsDeviceExistAsync(userId, deviceId);

            result.Should().NotBeNull();
            result!.Id.Should().Be(session.Id);
        }
    }

}
