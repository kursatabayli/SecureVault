using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Infrastructure.Helpers;
using SecureVault.App.Infrastructure.HttpHandlers;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin
{
    public class QrHubConnection : IQrHubConnection
    {
        private HubConnection _hubConnection;
        private readonly IHttpHandlerPipelineBuilder _pipelineBuilder;
        private readonly ILogger<QrHubConnection> _logger;
        private readonly string _hubUrl;
        private string _channelId;

        public event Func<string, string, Task> OnMessageReceived;
        public event Func<string, Task> OnErrorReceived;

        public QrHubConnection(
            IHttpHandlerPipelineBuilder pipelineBuilder,
            IOptions<ApiSettings> apiSettings,
            IOptions<QrCodeSettings> qrCodeSettings,
            ILogger<QrHubConnection> logger)
        {
            _pipelineBuilder = pipelineBuilder;
            _logger = logger;

            var baseUrl = new Uri(apiSettings.Value.BaseUrl);
            var fullHubUri = new Uri(baseUrl, qrCodeSettings.Value.HubPath);
            _hubUrl = fullHubUri.ToString();
        }

        public async Task ConnectAndJoinChannelAsync(string channelId, CancellationToken cancellationToken = default)
        {
            if (_hubConnection is not null && _hubConnection.State != HubConnectionState.Disconnected)
            {
                _logger.LogWarning("Hub connection already exists and is not in a disconnected state.");
                return;
            }

            _channelId = channelId;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    options.HttpMessageHandlerFactory = _ => _pipelineBuilder.CreatePipeline();
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string, string>("ReceiveMessage", async (type, payload) =>
            {
                _logger.LogInformation("Message received. Type: {MessageType}", type);
                if (OnMessageReceived != null)
                    await OnMessageReceived.Invoke(type, payload);
            });

            _hubConnection.On<string>("Error", async (errorMessage) =>
            {
                _logger.LogError("Server-side hub error: {ErrorMessage}", errorMessage);
                if (OnErrorReceived != null)
                    await OnErrorReceived.Invoke(errorMessage);
            });

            try
            {
                _logger.LogInformation("Starting hub connection to {HubUrl}...", _hubUrl);
                await _hubConnection.StartAsync(cancellationToken);
                _logger.LogInformation("Hub connection started successfully. Joining channel {ChannelId}...", _channelId);
                await _hubConnection.InvokeAsync("JoinChannel", _channelId, cancellationToken);
                _logger.LogInformation("Successfully joined channel {ChannelId}.", _channelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to hub or join channel {ChannelId}.", _channelId);
                // Hata durumunda event'i tetikleyebilir veya exception'ı yeniden fırlatabiliriz.
                // Burada yeniden fırlatmak, çağıran tarafın (orkestratörün) hatayı yakalamasını sağlar.
                throw;
            }
        }

        public async Task SendMessageAsync(string messageType, string payload, CancellationToken cancellationToken = default)
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                _logger.LogError("Cannot send message. Hub is not connected.");
                throw new InvalidOperationException("Hub is not connected.");
            }

            try
            {
                _logger.LogInformation("Sending message to channel {ChannelId}. Type: {MessageType}", _channelId, messageType);
                await _hubConnection.InvokeAsync("SendMessageToChannel", _channelId, messageType, payload, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message of type {MessageType} to channel {ChannelId}", messageType, _channelId);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                _logger.LogInformation("Disposing hub connection.");
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }
    }
}
