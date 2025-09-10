using Microsoft.Extensions.Options;
using SecureVault.Interaction.Api.Features.QrLogin.Contracts;
using SecureVault.Interaction.Api.Features.QrLogin.Services;
using SecureVault.Interaction.Api.Features.Sync.BackgroundServices;
using SecureVault.Interaction.Api.Features.Sync.Contracts;
using SecureVault.Interaction.Api.Features.Sync.EventHandlers;
using SecureVault.Interaction.Api.Features.Sync.Services;
using SecureVault.Shared.Contracts.Events;
using SecureVault.Shared.RabbitMQ.Connector;
using SecureVault.Shared.RabbitMQ.Contracts;
using SecureVault.Shared.RabbitMQ.Implementations;
using SecureVault.Shared.RabbitMQ.Options;

namespace SecureVault.Interaction.Api.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
            services.AddHostedService<RabbitMqConnector>();
            services.AddScoped<UserActivityOccurredIntegrationEventHandler>();
            services.AddScoped<INotificationService, SignalRNotificationService>();
            services.AddSingleton<IUserConnectionManager, InMemoryUserConnectionManager>();

            services.AddHostedService(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<GenericRabbitMqConsumer<UserActivityOccurredIntegrationEvent, UserActivityOccurredIntegrationEventHandler>>>();
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var connection = sp.GetRequiredService<IRabbitMqConnection>();
                var rabbitMqOptions = sp.GetRequiredService<IOptions<RabbitMqOptions>>();

                var consumerOptions = new RabbitMqConsumerOptions
                {
                    ExchangeName = rabbitMqOptions.Value.ExchangeName,
                    QueueName = "sync.user.activity.updated",
                    RoutingKey = "vault.#" 
                };

                return new GenericRabbitMqConsumer<UserActivityOccurredIntegrationEvent, UserActivityOccurredIntegrationEventHandler>(logger, scopeFactory, connection, consumerOptions);
            });


            services.AddMemoryCache();
            services.AddScoped<IQrLoginChannelService, InMemoryQrLoginChannelService>();

            return services;
        }
    }
}
