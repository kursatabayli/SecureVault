using Microsoft.EntityFrameworkCore;
using SecureVault.Identity.Infrastructure.Context;

namespace SecureVault.Identity.Api.Extensions
{
    public static class DatabaseExtension
    {
        public static IServiceCollection AddDbContextConfiguration(
                    this IServiceCollection services,
                    IHostApplicationBuilder builder)
        {
            builder.AddNpgsqlDbContext<AppDbContext>("identity-db",
                configureDbContextOptions: options =>
                {
                    options.UseSnakeCaseNamingConvention();
                });

            return services;
        }
    }
}
