using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Identity.Infrastructure.Context;
using SecureVault.Identity.Infrastructure.Services;
using System;

namespace SecureVault.Identity.Infrastructure.Tests.Services
{
    public class UnitOfWorkTests
    {
        private readonly AppDbContext _context;
        private readonly UnitOfWork _unitOfWork;

        public UnitOfWorkTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _unitOfWork = new UnitOfWork(_context);
        }

        [Fact]
        public async Task SaveChangesAsync_Should_PersistChangesToDatabase()
        {
            var user = User.Create("test@uow.com", [], [], new UserInfo
            {
                Name = "Test",
                Surname = "UoW",
                PhoneNumber = "0987654321"
            });
            
            _context.Users.Add(user);

            await _unitOfWork.SaveChangesAsync();

            var userInDb = await _context.Users.FindAsync(user.Id);
            userInDb.Should().NotBeNull();
            userInDb!.Id.Should().Be(user.Id);
        }
    }

}
