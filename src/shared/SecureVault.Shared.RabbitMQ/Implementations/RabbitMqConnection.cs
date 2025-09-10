using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using SecureVault.Shared.RabbitMQ.Contracts;
using System.Net.Sockets;

namespace SecureVault.Shared.RabbitMQ.Implementations
{
    public class RabbitMqConnection : IRabbitMqConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger _logger;
        private IConnection? _connection;
        private bool _disposed;
        private readonly SemaphoreSlim _refreshTokenLock = new(1, 1);

        public RabbitMqConnection(IConnectionFactory connectionFactory, ILogger<RabbitMqConnection> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

        public async Task<IChannel> CreateModelAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("RabbitMQ bağlantısı aktif değil.");
            }
            return await _connection.CreateChannelAsync(null, cancellationToken);
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected) return;

            await _refreshTokenLock.WaitAsync(cancellationToken);
            try
            {
                var policy = Policy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning(ex, "RabbitMQ bağlantısı kurulamadı. {TimeOut}s sonra yeniden denenecek.", time.TotalSeconds);
                    });

                await policy.Execute(async () =>
                {
                    _connection = await _connectionFactory.CreateConnectionAsync();
                });

                if (IsConnected)
                {
                    _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
                    _connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
                    _connection.ConnectionBlockedAsync += OnConnectionBlockedAsync;
                    _logger.LogInformation("RabbitMQ bağlantısı başarıyla kuruldu: {Host}", _connection.Endpoint.HostName);
                }
                else
                {
                    _logger.LogCritical("RabbitMQ bağlantısı kurulamadı.");
                }
            }
            finally
            {
                _refreshTokenLock.Release();
            }
        }

        private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs reason)
        {
            _logger.LogWarning("RabbitMQ bağlantısı kapandı. Sebep: {Reason}", reason.ReplyText);
            return Task.CompletedTask;
        }
        private Task OnCallbackExceptionAsync(object sender, CallbackExceptionEventArgs e)
        {
            _logger.LogWarning(e.Exception, "RabbitMQ callback hatası.");
            return Task.CompletedTask;
        }
        private Task OnConnectionBlockedAsync(object sender, ConnectionBlockedEventArgs e)
        {
            _logger.LogWarning("RabbitMQ bağlantısı bloklandı. Sebep: {Reason}", e.Reason);
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                if (_connection != null)
                {
                    _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
                    _connection.CallbackExceptionAsync -= OnCallbackExceptionAsync;
                    _connection.ConnectionBlockedAsync -= OnConnectionBlockedAsync;

                    await _connection.DisposeAsync();
                }
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex, "RabbitMQ bağlantısı kapatılırken hata oluştu.");
            }

            _refreshTokenLock?.Dispose();
        }
    }
}