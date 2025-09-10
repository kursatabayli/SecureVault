using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Results;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Identity.Infrastructure.Context;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

namespace SecureVault.Identity.Api.Tests.Controllers
{
    public class UserSessionControllerTests : IClassFixture<IdentityApiFactory>
    {
        private readonly HttpClient _client;
        private readonly IdentityApiFactory _factory;

        public UserSessionControllerTests(IdentityApiFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        private string GenerateTestToken(Guid userId, string issuer = "TestIssuer", string audience = "TestAudience")
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKeyForTestingPurposes123!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public async Task GetAllUserSessions_Should_ReturnUnauthorized_WhenNoTokenIsProvided()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/UserSession");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAllUserSessions_Should_ReturnOkAndSessions_WhenTokenIsValid()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var token = GenerateTestToken(userId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var session = UserSession.Create(userId, "jti", "access_jti", new DeviceDetail(), null, DateTimeOffset.UtcNow.AddDays(1), false, false);
            dbContext.UserSessions.Add(session);
            await dbContext.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync("/api/UserSession");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var sessions = await response.Content.ReadFromJsonAsync<List<UserSessionResult>>();
            sessions.Should().NotBeNull();
            sessions.Should().HaveCount(1);
            sessions!.First().UserId.Should().Be(userId);
        }

        [Fact]
        public async Task RevokeSession_Should_ReturnNoContent_WhenSessionIsRevokedSuccessfully()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            var userId = Guid.NewGuid();
            var token = GenerateTestToken(userId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var session = UserSession.Create(userId, "jti", "access_jti", new DeviceDetail(), null, DateTimeOffset.UtcNow.AddDays(1), false, false);
            dbContext.UserSessions.Add(session);
            await dbContext.SaveChangesAsync();

            // Act
            var response = await _client.DeleteAsync($"/api/UserSession/{session.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var revokedSession = await dbContext.UserSessions.FindAsync(session.Id);
            revokedSession.Should().BeNull();
        }

        [Fact]
        public async Task RevokeSession_Should_ReturnBadRequest_WhenTryingToRevokeAnotherUsersSession()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            var ownerUserId = Guid.NewGuid();
            var attackerUserId = Guid.NewGuid();

            var token = GenerateTestToken(attackerUserId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Oturum sahibi başka bir kullanıcı
            var session = UserSession.Create(ownerUserId, "jti", "access_jti", new DeviceDetail(), null, DateTimeOffset.UtcNow.AddDays(1), false, false);
            dbContext.UserSessions.Add(session);
            await dbContext.SaveChangesAsync();

            // Act
            var response = await _client.DeleteAsync($"/api/UserSession/{session.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

}
