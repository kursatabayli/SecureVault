using Microsoft.EntityFrameworkCore;
using SecureVault.Identity.Infrastructure.Context;

namespace SecureVault.Identity.Api.Extensions
{
    public static class DatabaseExtension
    {
        public static IServiceCollection AddDbContextConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure())
                        .UseSnakeCaseNamingConvention()
            );

            return services;
        }
    }
}
