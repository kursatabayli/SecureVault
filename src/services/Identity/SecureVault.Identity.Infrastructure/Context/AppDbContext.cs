using Microsoft.EntityFrameworkCore;
using SecureVault.Identity.Domain.Entities;

namespace SecureVault.Identity.Infrastructure.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<UserRecoveryData> UserRecoveryData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(builder =>
            {
                builder.OwnsOne(user => user.UserInfo, userInfoBuilder =>
                {
                    userInfoBuilder.ToJson(); 
                });
            });

            modelBuilder.Entity<UserSession>(builder =>
            {
                builder.OwnsOne(session => session.DeviceDetails, deviceDetailsBuilder =>
                {
                    deviceDetailsBuilder.ToJson();
                });
            });

            modelBuilder.Entity<User>()
                .HasOne(u => u.UserRecoveryData)
                .WithOne(rd => rd.User)
                .HasForeignKey<UserRecoveryData>(rd => rd.UserId);
        }
    }
}
