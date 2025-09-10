using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SecureVault.Shared.Contracts.Events;
using SecureVault.Shared.RabbitMQ.Contracts;
using SecureVault.Shared.RabbitMQ.Options;
using System.Text;
using System.Text.Json;

namespace SecureVault.Shared.RabbitMQ.Implementations
{
    public class RabbitMqEventPublisher : IEventPublisher
    {
        private readonly IRabbitMqConnection _connection;
        private readonly ILogger<RabbitMqEventPublisher> _logger;
        private readonly RabbitMqOptions _options;

        public RabbitMqEventPublisher(
            IRabbitMqConnection connection,
            ILogger<RabbitMqEventPublisher> logger,
            IOptions<RabbitMqOptions> options)
        {
            _connection = connection;
            _logger = logger;
            _options = options.Value;
        }

        public async Task PublishAsync<T>(T @event, string routingKey, CancellationToken cancellationToken = default)
            where T : IIntegrationEvent
        {
            if (!_connection.IsConnected)
            {
                _logger.LogError("RabbitMQ'ya mesaj yayınlanamadı çünkü bağlantı aktif değil.");
                throw new InvalidOperationException("RabbitMQ bağlantısı aktif değil.");
            }

            await using var channel = await _connection.CreateModelAsync(cancellationToken);

            await channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                cancellationToken: cancellationToken
            );

            var message = JsonSerializer.Serialize(@event, @event.GetType()); // Polymorphism için event.GetType() önemli
            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = @event.Id.ToString(),
                Timestamp = new AmqpTimestamp(new DateTimeOffset(@event.CreationDate).ToUnixTimeSeconds()),
                ContentType = "application/json"
            };

            _logger.LogInformation("RabbitMQ'ya mesaj yayınlanıyor: Exchange={Exchange}, RoutingKey={RoutingKey}", _options.ExchangeName, routingKey);

            await channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken
            );
        }
    }
}
