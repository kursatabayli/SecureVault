using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Services;
using SecureVault.Identity.Infrastructure.Repositories;
using SecureVault.Identity.Infrastructure.Services;
using SecureVault.Shared.RabbitMQ.Connector;
using SecureVault.Shared.RabbitMQ.Contracts;
using SecureVault.Shared.RabbitMQ.Implementations;

namespace SecureVault.Identity.Api.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            //Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IHashService, HashService>();
            services.AddScoped<IEcdsaVerificationService, EcdsaVerificationService>();
            services.AddScoped<ICacheService, RedisCacheService>();

            //Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserSessionRepository, UserSessionRepository>();
            services.AddScoped<IUserRecoveryDataRepository, UserRecoveryDataRepository>();

            //Application Services
            services.AddScoped<IUserSessionService, UserSessionService>();
            services.AddScoped<ITokenValidationService, TokenValidationService>();

            //RabbitMQ
            services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
            services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
            services.AddHostedService<RabbitMqConnector>();


            return services;
        }
    }
}
