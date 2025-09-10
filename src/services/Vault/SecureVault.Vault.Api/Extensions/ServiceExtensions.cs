using SecureVault.Shared.RabbitMQ.Connector;
using SecureVault.Shared.RabbitMQ.Contracts;
using SecureVault.Shared.RabbitMQ.Implementations;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Infrastructure.Context;
using SecureVault.Vault.Infrastructure.Repositories;

namespace SecureVault.Vault.Api.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IVaultItemsRepository, VaultItemsRepository>();

            services.AddScoped<MongoDbContext>();

            //RabbitMQ
            services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
            services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
            services.AddHostedService<RabbitMqConnector>();
            return services;
        }
    }
}
