using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SecureVault.Identity.Application.Contracts.DTOs.RegisterDto;
using SecureVault.Identity.Application.Contracts.DTOs.UserDto;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Identity.Infrastructure.Context;
using System.Net;
using System.Net.Http.Json;

namespace SecureVault.Identity.Api.Tests.Controllers
{
    public class RegisterControllerTests : IClassFixture<IdentityApiFactory>
    {
        private readonly HttpClient _client;
        private readonly IdentityApiFactory _factory;

        public RegisterControllerTests(IdentityApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Register_Should_ReturnOk_WhenRegistrationIsSuccessful()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            var registerDto = new RegisterUserDto
            {
                Email = "newuser@test.com",
                PublicKey = System.Text.Encoding.UTF8.GetBytes("public_key_string"),
                Salt = System.Text.Encoding.UTF8.GetBytes("salt_string"),
                UserInfo = new UserInfoDto
                {
                    Name = "Test",
                    Surname = "User",
                    PhoneNumber = "1234567890"
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Veritabanında kullanıcının gerçekten oluşturulduğunu doğrula
            var userInDb = dbContext.Users.FirstOrDefault(u => u.Email == "newuser@test.com");
            userInDb.Should().NotBeNull();
            userInDb!.UserInfo.Name.Should().Be("Test");
        }

        [Fact]
        public async Task Register_Should_ReturnBadRequest_WhenEmailIsAlreadyTaken()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            // Mevcut kullanıcıyı veritabanına ekle
            var existingUser = User.Create("existing@test.com", [], [], new UserInfo());
            dbContext.Users.Add(existingUser);
            await dbContext.SaveChangesAsync();

            var registerDto = new RegisterUserDto
            {
                Email = "newuser@test.com",
                PublicKey = System.Text.Encoding.UTF8.GetBytes("public_key_string"),
                Salt = System.Text.Encoding.UTF8.GetBytes("salt_string"),
                UserInfo = new UserInfoDto
                {
                    Name = "Test",
                    Surname = "User",
                    PhoneNumber = "1234567890"
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
