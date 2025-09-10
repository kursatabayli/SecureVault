using Microsoft.EntityFrameworkCore;
using SecureVault.App.Domain.Entities;

namespace SecureVault.App.Infrastructure.Context
{
    public class SecureVaultDbContext : DbContext
    {
        public SecureVaultDbContext(DbContextOptions<SecureVaultDbContext> options) : base(options)
        {
        }

        public DbSet<PasswordEntity> Passwords { get; set; }
        public DbSet<TwoFactorAuthCodeEntity> TwoFactorAuthCodes { get; set; }
    }
}
