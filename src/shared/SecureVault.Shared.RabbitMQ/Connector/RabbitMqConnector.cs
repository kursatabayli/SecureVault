using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecureVault.Shared.RabbitMQ.Contracts;

namespace SecureVault.Shared.RabbitMQ.Connector
{
    public class RabbitMqConnector : IHostedService
    {
        private readonly IRabbitMqConnection _rabbitMqConnection;
        private readonly ILogger<RabbitMqConnector> _logger;

        public RabbitMqConnector(IRabbitMqConnection rabbitMqConnection, ILogger<RabbitMqConnector> logger)
        {
            _rabbitMqConnection = rabbitMqConnection;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Uygulama başlıyor, RabbitMQ bağlantısı kuruluyor...");
            await _rabbitMqConnection.ConnectAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Uygulama durduruluyor, RabbitMQ bağlantısı kapatılıyor.");
            if (_rabbitMqConnection != null)
                await _rabbitMqConnection.DisposeAsync();
        }
    }
}
