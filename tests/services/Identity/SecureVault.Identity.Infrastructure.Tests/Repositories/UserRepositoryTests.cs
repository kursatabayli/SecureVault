using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Identity.Infrastructure.Context;
using SecureVault.Identity.Infrastructure.Repositories;

namespace SecureVault.Identity.Infrastructure.Tests.Repositories
{
    public class UserRepositoryTests
    {
        private readonly AppDbContext _context;
        private readonly UserRepository _userRepository;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _userRepository = new UserRepository(_context);
        }

        private User CreateTestUser(string email)
        {
            return User.Create(email, [1], [2], new UserInfo { Name = "Test", Surname = "User", PhoneNumber = "1234567890" });
        }

        [Fact]
        public async Task AddAsync_Should_AddUserToDatabase()
        {
            var user = CreateTestUser("add@example.com");

            await _userRepository.AddAsync(user);
            await _context.SaveChangesAsync();

            var userInDb = await _context.Users.FindAsync(user.Id);
            userInDb.Should().NotBeNull();
            userInDb.Should().BeEquivalentTo(user);
        }

        [Fact]
        public async Task GetByEmailAsync_Should_ReturnUser_WhenUserExists()
        {
            var user = CreateTestUser("findbyemail@example.com");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _userRepository.GetByEmailAsync("findbyemail@example.com");

            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetByEmailAsync_Should_ReturnNull_WhenUserDoesNotExist()
        {
            var result = await _userRepository.GetByEmailAsync("nonexistent@example.com");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserWithUserInfoAsync_Should_IncludeUserInfo()
        {
            var user = CreateTestUser("userwithinfo@example.com");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _userRepository.GetUserWithUserInfoAsync(user.Id);

            result.Should().NotBeNull();
            result!.UserInfo.Should().NotBeNull();
            result.UserInfo.Name.Should().Be("Test");
        }
    }

}
