using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Services;
using SecureVault.Identity.Infrastructure.Repositories;
using SecureVault.Identity.Infrastructure.Services;

namespace SecureVault.Identity.Api.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            //Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IEcdsaVerificationService, EcdsaVerificationService>();
            services.AddScoped<ICacheService, RedisCacheService>();

            //Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserSessionRepository, UserSessionRepository>();
            services.AddScoped<IUserRecoveryDataRepository, UserRecoveryDataRepository>();

            //Application Services
            services.AddScoped<IUserSessionService, UserSessionService>();
            services.AddScoped<ITokenValidationService, TokenValidationService>();

            return services;
        }
    }
}
