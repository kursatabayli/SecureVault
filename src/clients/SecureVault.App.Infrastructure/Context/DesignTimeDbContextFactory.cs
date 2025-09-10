using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SecureVault.App.Infrastructure.Context
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SecureVaultDbContext>
    {
        public SecureVaultDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SecureVaultDbContext>();
            optionsBuilder.UseSqlite("Filename=temp_for_migrations.db3");
            return new SecureVaultDbContext(optionsBuilder.Options);
        }
    }
}
