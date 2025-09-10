using FluentAssertions;
using SecureVault.Identity.Domain.Entities;

namespace SecureVault.Identity.Domain.Tests
{
    public class UserTests
    {
        [Fact]
        public void Create_Should_InitializeUser_Correctly()
        {
            var email = "Test.User@example.com";
            var publicKey = new byte[] { 1, 2, 3 };
            var salt = new byte[] { 4, 5, 6 };
            var userInfo = new UserInfo { Name = "Test", Surname = "User" };

            var user = User.Create(email, publicKey, salt, userInfo);

            user.Should().NotBeNull();
            user.Id.Should().NotBe(Guid.Empty);
            user.Email.Should().Be(email.ToLowerInvariant());
            user.PublicKey.Should().BeEquivalentTo(publicKey);
            user.Salt.Should().BeEquivalentTo(salt);
            user.UserInfo.Should().Be(userInfo);
            user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
            user.UpdatedAt.Should().Be(user.CreatedAt);
        }

        [Fact]
        public void UpdateUserInfo_Should_UpdateInfo_And_SetUpdatedAt()
        {
            var user = User.Create("test@example.com", [], [], new UserInfo { Name = "Initial", Surname = "Name" });
            var initialUpdatedAt = user.UpdatedAt;
            var newUserInfo = new UserInfo { Name = "Updated", Surname = "Name" };

            Thread.Sleep(10);

            user.UpdateUserInfo(newUserInfo);

            user.UserInfo.Should().Be(newUserInfo);
            user.UpdatedAt.Should().BeAfter(initialUpdatedAt);
        }
    }
}
