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
                builder.HasKey(u => u.Id);

                builder.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                builder.Property(u => u.PublicKey)
                    .IsRequired();

                builder.Property(u => u.Salt)
                    .IsRequired();

                builder.OwnsOne(user => user.UserInfo, userInfoBuilder =>
                {
                    userInfoBuilder.ToJson("user_info");
                });

                builder.Property(u => u.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");

                builder.Property(u => u.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");

                builder.HasIndex(u => u.Email).IsUnique();

                builder.HasMany(u => u.UserSessions)
                    .WithOne(s => s.User)
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserSession>(builder =>
            {
                builder.HasKey(s => s.Id);

                builder.Property(s => s.IpAddress)
                    .HasMaxLength(45);

                builder.Property(s => s.ExpiresAt)
                    .IsRequired();

                builder.Property(s => s.IsRevoked)
                    .IsRequired()
                    .HasDefaultValue(false);

                builder.Property(s => s.IsPersistent)
                    .IsRequired()
                    .HasDefaultValue(false);

                builder.Property(s => s.RefreshTokenJti)
                    .IsRequired();

                builder.OwnsOne(session => session.DeviceDetails, deviceDetailsBuilder =>
                {
                    deviceDetailsBuilder.ToJson("device_details");
                });

                builder.Property(s => s.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");

                builder.HasIndex(s => s.UserId);
                builder.HasIndex(s => s.RefreshTokenJti);
            });

            modelBuilder.Entity<UserRecoveryData>(builder =>
            {
                builder.HasKey(rd => rd.UserId);

                builder.Property(rd => rd.RecoveryData)
                    .IsRequired();

                builder.Property(rd => rd.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");

                builder.Property(rd => rd.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");

                builder.HasOne(rd => rd.User)
                    .WithOne(u => u.UserRecoveryData)
                    .HasForeignKey<UserRecoveryData>(rd => rd.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
