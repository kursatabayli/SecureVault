using FluentAssertions;
using SecureVault.Identity.Domain.Entities;

namespace SecureVault.Identity.Domain.Tests
{
    public class UserSessionTests
    {
        private UserSession CreateTestSession()
        {
            return UserSession.Create(
                userId: Guid.NewGuid(),
                refreshTokenJti: "refresh_jti_1",
                accessTokenJti: "access_jti_1",
                deviceDetails: new DeviceDetail { UniqueDeviceId = "device-123" },
                ipAddress: "127.0.0.1",
                expiresAt: DateTimeOffset.UtcNow.AddDays(7),
                isRevoked: false,
                isPersistent: true
            );
        }

        [Fact]
        public void Create_Should_InitializeUserSession_Correctly()
        {
            var userId = Guid.NewGuid();
            var refreshTokenJti = "refresh_jti_123";
            var accessTokenJti = "access_jti_123";
            var deviceDetails = new DeviceDetail { UniqueDeviceId = "device-abc" };
            var ipAddress = "192.168.1.1";
            var expiresAt = DateTimeOffset.UtcNow.AddDays(30);
            var isPersistent = true;

            var session = UserSession.Create(userId, refreshTokenJti, accessTokenJti, deviceDetails, ipAddress, expiresAt, false, isPersistent);

            session.Should().NotBeNull();
            session.Id.Should().NotBe(Guid.Empty);
            session.UserId.Should().Be(userId);
            session.RefreshTokenJti.Should().Be(refreshTokenJti);
            session.AccessTokenJti.Should().Be(accessTokenJti);
            session.DeviceDetails.Should().Be(deviceDetails);
            session.IpAddress.Should().Be(ipAddress);
            session.ExpiresAt.Should().Be(expiresAt);
            session.IsPersistent.Should().Be(isPersistent);
            session.IsRevoked.Should().BeFalse();
            session.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
            session.LastUsedAt.Should().Be(session.CreatedAt);
        }

        [Fact]
        public void Update_Should_UpdateSessionDetails_And_ResetRevokedStatus()
        {
            var session = CreateTestSession();
            session.Revoke();
            var initialLastUsedAt = session.LastUsedAt;

            var newRefreshTokenJti = "new_refresh_jti";
            var newAccessTokenJti = "new_access_jti";
            var newIpAddress = "192.168.1.2";
            var newExpiresAt = DateTimeOffset.UtcNow.AddDays(15);

            Thread.Sleep(10);

            session.Update(newRefreshTokenJti, newAccessTokenJti, newIpAddress, newExpiresAt, false);

            session.RefreshTokenJti.Should().Be(newRefreshTokenJti);
            session.AccessTokenJti.Should().Be(newAccessTokenJti);
            session.IpAddress.Should().Be(newIpAddress);
            session.ExpiresAt.Should().Be(newExpiresAt);
            session.IsPersistent.Should().BeFalse();
            session.IsRevoked.Should().BeFalse();
            session.LastUsedAt.Should().BeAfter(initialLastUsedAt.Value);
        }

        [Fact]
        public void Revoke_Should_SetIsRevokedToTrue()
        {
            var session = CreateTestSession();

            session.Revoke();

            session.IsRevoked.Should().BeTrue();
        }

        [Fact]
        public void Revoke_WhenAlreadyRevoked_Should_RemainRevoked()
        {
            var session = CreateTestSession();
            session.Revoke();

            session.Revoke();

            session.IsRevoked.Should().BeTrue();
        }
    }


}