using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Identity.Infrastructure.Helpers;
using SecureVault.Identity.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SecureVault.Identity.Infrastructure.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly AuthService _authService;
        private readonly JwtSettings _jwtSettings;

        public AuthServiceTests()
        {
            _jwtSettings = new JwtSettings
            {
                Key = "SuperSecretKeyForTestingPurposes123!",
                RefreshTokenKey = "SuperSecretRefreshTokenKeyForTesting123!",
                Issuer = "TestIssuer",
                Audience = "TestAudience"
            };
            var options = Options.Create(_jwtSettings);
            var logger = new Mock<ILogger<AuthService>>().Object;
            _authService = new AuthService(options, logger);
        }

        [Fact]
        public void GenerateJwtTokenForUser_Should_CreateTokenWithCorrectClaims()
        {
            var user = User.Create("test@user.com", [], [], new UserInfo { Name = "Test", Surname = "User" });

            var (token, jti, expiration) = _authService.GenerateJwtTokenForUser(user);

            token.Should().NotBeNullOrWhiteSpace();
            jti.Should().NotBeNullOrWhiteSpace();
            expiration.Should().BeAfter(DateTime.UtcNow);

            var handler = new JwtSecurityTokenHandler();
            var decodedToken = handler.ReadJwtToken(token);

            decodedToken.Issuer.Should().Be(_jwtSettings.Issuer);
            decodedToken.Audiences.Should().Contain(_jwtSettings.Audience);
            decodedToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
            decodedToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
            decodedToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.UserInfo.Name);
        }

        [Theory]
        [InlineData(true, 30)]
        [InlineData(false, 1)]
        public void GenerateRefreshTokenJwt_Should_SetCorrectExpiration(bool rememberMe, int expectedExpirationHours)
        {
            var userId = Guid.NewGuid();

            var (token, _, expiration) = _authService.GenerateRefreshTokenJwt(userId, rememberMe);

            token.Should().NotBeNullOrWhiteSpace();
            var expectedDuration = rememberMe ? TimeSpan.FromDays(expectedExpirationHours) : TimeSpan.FromHours(expectedExpirationHours);
            expiration.Should().BeCloseTo(DateTime.UtcNow.Add(expectedDuration), TimeSpan.FromSeconds(5));
        }
    }

}
