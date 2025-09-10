using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SecureVault.Interaction.Api.Features.Sync.Contracts;
using SecureVault.Shared.Contracts.Events;
using SecureVault.Shared.RabbitMQ.Contracts;
using SecureVault.Shared.RabbitMQ.Options;
using System.Text;
using System.Text.Json;

namespace SecureVault.Interaction.Api.Features.Sync.BackgroundServices
{
    public class GenericRabbitMqConsumer<TEvent, THandler> : BackgroundService
        where TEvent : IIntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        private readonly ILogger<GenericRabbitMqConsumer<TEvent, THandler>> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IRabbitMqConnection _connection;
        private readonly RabbitMqConsumerOptions _options;
        private IChannel _channel;

        public GenericRabbitMqConsumer(
            ILogger<GenericRabbitMqConsumer<TEvent, THandler>> logger,
            IServiceScopeFactory scopeFactory,
            IRabbitMqConnection connection,
            RabbitMqConsumerOptions options)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _connection = connection;
            _options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            if (!_connection.IsConnected)
            {
                _logger.LogWarning("RabbitMQ bağlantısı henüz aktif değil. Consumer bekliyor...");
            }

            try
            {
                _channel = await _connection.CreateModelAsync(stoppingToken);

                await _channel.ExchangeDeclareAsync(exchange: _options.ExchangeName, type: ExchangeType.Topic, durable: true);

                var arguments = new Dictionary<string, object>
                    {
                        { "x-dead-letter-exchange", $"{_options.ExchangeName}-dlx" },
                        { "x-dead-letter-routing-key", $"dlq.{_options.RoutingKey}" }
                    };

                await _channel.QueueDeclareAsync(queue: _options.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
                await _channel.QueueBindAsync(queue: _options.QueueName, exchange: _options.ExchangeName, routingKey: _options.RoutingKey);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += OnMessageReceived;

                await _channel.BasicConsumeAsync(queue: _options.QueueName, autoAck: false, consumer: consumer);

                _logger.LogInformation("'{QueueName}' kuyruğu dinlenmeye başlandı. Olay Türü: {EventType}", _options.QueueName, typeof(TEvent).Name);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "RabbitMQ consumer ({EventType}) başlatılırken kritik bir hata oluştu.", typeof(TEvent).Name);
            }
        }

        private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
        {
            var messageJson = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            try
            {
                var integrationEvent = JsonSerializer.Deserialize<TEvent>(messageJson);
                if (integrationEvent is null)
                {
                    _logger.LogWarning("Mesaj {EventType} formatına dönüştürülemedi.", typeof(TEvent).Name);
                    await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
                    return;
                }

                _logger.LogInformation("{EventType} alındı. İşleniyor...", typeof(TEvent).Name);

                await using var scope = _scopeFactory.CreateAsyncScope();

                var handler = scope.ServiceProvider.GetRequiredService<THandler>();
                await handler.Handle(integrationEvent);

                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mesaj işlenirken hata oluştu ({EventType}). Mesaj DLX'e yönlendiriliyor. Mesaj: {Message}", typeof(TEvent).Name, messageJson);
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: false);
            }
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null)
            {
                await _channel.DisposeAsync();
            }

            await base.StopAsync(cancellationToken);

            _logger.LogInformation("RabbitMQ consumer durduruldu.");
        }
        public override void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
