using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SecureVault.Identity.Application.Contracts.DTOs.AuthDto;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Identity.Infrastructure.Context;
using System.Net;
using System.Net.Http.Json;

namespace SecureVault.Identity.Api.Tests.Controllers
{
    public class AuthControllerTests : IClassFixture<IdentityApiFactory>
    {
        private readonly HttpClient _client;
        private readonly IdentityApiFactory _factory;

        public AuthControllerTests(IdentityApiFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task RequestChallenge_Should_ReturnOk_WhenUserExists()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            var user = User.Create("test@challenge.com", [], [], new UserInfo());
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var response = await _client.GetAsync($"/api/Auth/challenge/{user.Email}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var challenge = await response.Content.ReadFromJsonAsync<ChallengeDto>();
            challenge.Should().NotBeNull();
            challenge!.Challenge.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task RequestChallenge_Should_ReturnBadRequest_WhenUserDoesNotExist()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            var response = await _client.GetAsync("/api/Auth/challenge/nonexistent@user.com");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}