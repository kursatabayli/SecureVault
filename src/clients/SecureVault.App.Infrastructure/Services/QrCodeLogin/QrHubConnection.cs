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
        private readonly IPublicHttpHandlerPipelineBuilder _pipelineBuilder;
        private readonly ILogger<QrHubConnection> _logger;
        private readonly string _hubUrl;
        private string _channelId;

        public event Func<string, string, Task> OnMessageReceived;
        public event Func<string, Task> OnErrorReceived;

        public QrHubConnection(
            IPublicHttpHandlerPipelineBuilder pipelineBuilder,
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
            if (await IsConnectionActiveAsync())
            {
                _logger.LogWarning("Hub connection already exists and is active.");
                return;
            }

            _channelId = channelId;

            try
            {
                await InitializeAndStartConnectionAsync(cancellationToken);

                _logger.LogInformation("Hub connection started successfully. Joining channel {ChannelId}...", _channelId);
                await _hubConnection.InvokeAsync("JoinChannel", _channelId, cancellationToken);
                _logger.LogInformation("Successfully joined channel {ChannelId}.", _channelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to hub or join channel {ChannelId}.", _channelId);
                await DisposeAsync();
                throw;
            }
        }

        public async Task<string> ConnectAndCreateChannelAsync(CancellationToken cancellationToken = default)
        {
            if (await IsConnectionActiveAsync())
            {
                _logger.LogWarning("Hub connection already exists and is active.");
                await DisposeAsync();
            }

            try
            {
                await InitializeAndStartConnectionAsync(cancellationToken);

                _logger.LogInformation("Hub connection started successfully. Creating new channel...");
                var newChannelId = await _hubConnection.InvokeAsync<string>("CreateChannel", cancellationToken);

                if (string.IsNullOrEmpty(newChannelId))
                {
                    throw new InvalidOperationException("Hub returned an invalid (null or empty) channel ID.");
                }

                _channelId = newChannelId;
                _logger.LogInformation("Successfully created and joined channel {ChannelId}.", _channelId);
                return _channelId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to hub or create channel.");
                await DisposeAsync();
                throw;
            }
        }

        public async Task<string> SwitchChannelAsync(CancellationToken cancellationToken = default)
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                _logger.LogError("Cannot switch channel. Hub is not connected.");
                throw new InvalidOperationException("Hub is not connected.");
            }

            try
            {
                _logger.LogInformation("Requesting to switch channel. Leaving old channel {OldChannelId}...", _channelId);

                var newChannelId = await _hubConnection.InvokeAsync<string>("SwitchToNewChannel", cancellationToken);

                if (string.IsNullOrEmpty(newChannelId))
                {
                    throw new InvalidOperationException("Hub returned an invalid (null or empty) new channel ID during switch.");
                }

                var oldChannelId = _channelId;
                _channelId = newChannelId;

                _logger.LogInformation("Successfully switched from channel {OldChannelId} to new channel {NewChannelId}.", oldChannelId, _channelId);
                return _channelId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to switch channel. Current channel was {ChannelId}", _channelId);
                throw;
            }
        }

        private async Task InitializeAndStartConnectionAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Initializing new Hub connection to {HubUrl}...", _hubUrl);

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

            _hubConnection.Closed += OnConnectionClosed;
            _hubConnection.Reconnecting += OnReconnecting;
            _hubConnection.Reconnected += OnReconnected;

            await _hubConnection.StartAsync(cancellationToken);
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

        private Task<bool> IsConnectionActiveAsync()
        {
            return Task.FromResult(_hubConnection is not null && _hubConnection.State != HubConnectionState.Disconnected);
        }

        private Task OnConnectionClosed(Exception ex)
        {
            if (ex != null)
                _logger.LogError(ex, "Hub connection closed with error.");
            else
                _logger.LogInformation("Hub connection closed gracefully.");
            return Task.CompletedTask;
        }

        private Task OnReconnecting(Exception ex)
        {
            _logger.LogWarning(ex, "Hub connection is reconnecting...");
            return Task.CompletedTask;
        }

        private async Task OnReconnected(string newConnectionId)
        {
            _logger.LogInformation("Hub connection reestablished with new ConnectionId: {ConnectionId}.", newConnectionId);

            if (!string.IsNullOrEmpty(_channelId))
            {
                _logger.LogInformation("Re-joining channel {ChannelId} after reconnection.", _channelId);
                try
                {
                    await _hubConnection.InvokeAsync("JoinChannel", _channelId);
                    _logger.LogInformation("Successfully re-joined channel {ChannelId}.", _channelId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to re-join channel {ChannelId} after reconnection.", _channelId);
                    if (OnErrorReceived != null)
                        _logger.LogError(ex, "Failed to re-join channel {ChannelId} after reconnection.", _channelId);
                    if (OnErrorReceived != null)
                    {
                        await OnErrorReceived.Invoke("FailedToRejoinChannel");
                    }

                    _channelId = null;
                }
            }
        }
        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                _logger.LogInformation("Disposing hub connection.");

                _hubConnection.Closed -= OnConnectionClosed;
                _hubConnection.Reconnecting -= OnReconnecting;
                _hubConnection.Reconnected -= OnReconnected;

                await _hubConnection.DisposeAsync();
                _hubConnection = null;
                _channelId = null;
            }
        }
    }
}
