using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Infrastructure.Helpers;
using SecureVault.App.Infrastructure.HttpHandlers;
using System.Net.WebSockets;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin
{
    public class QrHubConnection : IQrHubConnection
    {
        private HubConnection _hubConnection;
        private readonly IPublicHttpHandlerPipelineBuilder _pipelineBuilder;
        private readonly IStorageService _storageService;
        private readonly IDpopProofService _dpopProofService;
        private readonly ILogger<QrHubConnection> _logger;
        private readonly string _hubUrl;
        private string _channelId;

        public event Func<string, string, Task> OnMessageReceived;
        public event Func<string, Task> OnErrorReceived;

        public QrHubConnection(
            IPublicHttpHandlerPipelineBuilder pipelineBuilder,
            IStorageService storageService,
            IDpopProofService dpopProofService,
            IOptions<ApiSettings> apiSettings,
            IOptions<QrCodeSettings> qrCodeSettings,
            ILogger<QrHubConnection> logger)
        {
            _pipelineBuilder = pipelineBuilder;
            _storageService = storageService;
            _dpopProofService = dpopProofService;
            _logger = logger;

            var baseUrl = new Uri(apiSettings.Value.BaseUrl);
            var fullHubUri = new Uri(baseUrl, qrCodeSettings.Value.HubPath);
            _hubUrl = fullHubUri.ToString();
        }

        public async Task<string> StartConnectionAsync(string? channelId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initializing new Hub connection to {HubUrl}...", _hubUrl);

            if(string.IsNullOrEmpty(channelId))
                channelId = Guid.CreateVersion7().ToString();
            _channelId = channelId;

            async ValueTask<WebSocket> CreateWebSocketFactoryAsync(WebSocketConnectionContext context, CancellationToken factoryCancellationToken)
            {
                var dws = new ClientWebSocket();

                try
                {
                    var currentAccessToken = await _storageService.GetAccessTokenAsync();

                    var uriBuilder = new UriBuilder(context.Uri.GetLeftPart(UriPartial.Path));
                    if (uriBuilder.Scheme == "wss")
                        uriBuilder.Scheme = "https";

                    var htu = uriBuilder.Uri.AbsoluteUri;
                    var dpopProof = await _dpopProofService.CreateProofAsync("GET", htu, currentAccessToken);

                    dws.Options.SetRequestHeader("DPoP", dpopProof);

                    if (!string.IsNullOrEmpty(currentAccessToken))
                        dws.Options.SetRequestHeader("Authorization", "DPoP " + currentAccessToken);


                    dws.Options.SetRequestHeader("X-Channel-Id", _channelId);

                    _logger.LogDebug("WebSocketFactory: DPoP başlıkları ile {Uri} adresine bağlanılıyor...", context.Uri);
                    await dws.ConnectAsync(context.Uri, factoryCancellationToken);
                    return dws;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WebSocketFactory içinde DPoP kanıtı oluşturulurken veya bağlanırken hata oluştu.");
                    dws.Dispose();
                    throw;
                }
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    options.Transports = HttpTransportType.WebSockets;
                    options.SkipNegotiation = true;
                    options.WebSocketFactory = CreateWebSocketFactoryAsync;
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
            return _channelId;
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

                var newChannelId = Guid.CreateVersion7().ToString();
                await _hubConnection.InvokeAsync<string>("SwitchToNewChannel", newChannelId, cancellationToken);

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

        private Task OnReconnected(string newConnectionId)
        {
            _logger.LogInformation("Hub connection reestablished with new ConnectionId: {ConnectionId}.", newConnectionId);
            _logger.LogInformation("Channel {ChannelId} was re-joined automatically via OnConnectedAsync logic.", _channelId);
            return Task.CompletedTask;
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
