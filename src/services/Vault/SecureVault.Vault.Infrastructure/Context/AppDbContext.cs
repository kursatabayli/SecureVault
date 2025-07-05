using Microsoft.EntityFrameworkCore;
using SecureVault.Vault.Domain.Entities;

namespace SecureVault.Vault.Infrastructure.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<VaultItem> VaultItems { get; set; }
    }
}
