using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureVault.App.Application.Contracts.Abstractions.Interaction;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Infrastructure.Helpers;
using SecureVault.App.Infrastructure.HttpHandlers;
using System.Net.WebSockets;

namespace SecureVault.App.Infrastructure.Services.Interaction
{
    public class InteractionConnectionService : IInteractionConnectionService
    {
        private readonly ILogger<InteractionConnectionService> _logger;
        private readonly IProtectedHttpHandlerPipelineBuilder _pipelineBuilder;
        private readonly IStorageService _storageService;
        private readonly IDpopProofService _dpopProofService;
        private readonly IEnumerable<ISignalRHubEventHandler> _hubEventHandlers;
        private HubConnection _connection;
        private readonly string _hubUrl;

        public InteractionConnectionService(ILogger<InteractionConnectionService> logger,
         IProtectedHttpHandlerPipelineBuilder pipelineBuilder,
         IStorageService storageService,
         IDpopProofService dpopProofService,
         IEnumerable<ISignalRHubEventHandler> hubEventHandlers,
         IOptions<ApiSettings> apiSettings,
         IOptions<SignalRSettings> signalRSettings)
        {
            _logger = logger;
            _pipelineBuilder = pipelineBuilder;
            _storageService = storageService;
            _dpopProofService = dpopProofService;
            _hubEventHandlers = hubEventHandlers;
            var baseUrl = new Uri(apiSettings.Value.BaseUrl);
            var fullHubUri = new Uri(baseUrl, signalRSettings.Value.HubPath);
            _hubUrl = fullHubUri.ToString();
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_connection is { State: HubConnectionState.Connected or HubConnectionState.Connecting })
            {
                _logger.LogInformation("SignalR bağlantısı zaten aktif veya kuruluyor.");
                return;
            }

            async ValueTask<WebSocket> CreateWebSocketFactoryAsync(WebSocketConnectionContext context, CancellationToken factoryCancellationToken)
            {
                var dws = new ClientWebSocket();

                try
                {
                    var currentAccessToken = await _storageService.GetAccessTokenAsync();
                    if (string.IsNullOrEmpty(currentAccessToken))
                    {
                        _logger.LogError("WebSocketFactory: Access Token bulunamadı.");
                        throw new InvalidOperationException("WebSocketFactory: Access Token bulunamadı.");
                    }

                    var uriBuilder = new UriBuilder(context.Uri.GetLeftPart(UriPartial.Path));
                    if (uriBuilder.Scheme == "wss")
                        uriBuilder.Scheme = "https";

                    var htu = uriBuilder.Uri.AbsoluteUri;
                    var dpopProof = await _dpopProofService.CreateProofAsync("GET", htu, currentAccessToken);

                    dws.Options.SetRequestHeader("DPoP", dpopProof);
                    dws.Options.SetRequestHeader("Authorization", "DPoP " + currentAccessToken);

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

            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    options.Transports = HttpTransportType.WebSockets;
                    options.SkipNegotiation = true;
                    options.HttpMessageHandlerFactory = _ => _pipelineBuilder.CreatePipeline();
                    options.WebSocketFactory = CreateWebSocketFactoryAsync;
                })
                .WithAutomaticReconnect()
                .Build();

            RegisterAllHandlers();

            try
            {
                _logger.LogInformation("SignalR Hub'ına bağlanılıyor...");
                await _connection.StartAsync(cancellationToken);
                _logger.LogInformation("SignalR Hub'ına başarıyla bağlanıldı. ConnectionId: {ConnectionId}", _connection.ConnectionId);
                await RegisterDeviceAfterConnection(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR Hub'ına bağlanırken hata oluştu.");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connection is { State: HubConnectionState.Connected })
            {
                _logger.LogInformation("SignalR bağlantısı kesiliyor...");
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _logger.LogInformation("SignalR bağlantısı başarıyla kesildi.");
            }
        }

        public async Task NotifySyncRequiredAsync(CancellationToken cancellationToken = default)
        {
            if (_connection is null || _connection.State != HubConnectionState.Connected)
            {
                _logger.LogWarning("SignalR bağlantısı aktif değil. Senkronizasyon bildirimi gönderilemedi.");
                return;
            }

            try
            {
                await _connection.InvokeAsync("NotifySyncRequired", cancellationToken);
                _logger.LogInformation("Sunucuya 'NotifySyncRequired' bildirimi başarıyla gönderildi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "'NotifySyncRequired' bildirimi gönderilirken hata oluştu.");
            }
        }

        public async Task NotifyUserSessionRevokedAsync(string userDeviceId, CancellationToken cancellationToken = default)
        {
            if (_connection is null || _connection.State != HubConnectionState.Connected)
            {
                _logger.LogWarning("SignalR bağlantısı aktif değil. Oturum iptal bildirimi gönderilemedi.");
                return;
            }

            try
            {
                await _connection.InvokeAsync("NotifyUserSessionRevoked", userDeviceId, cancellationToken);
                _logger.LogInformation("Sunucuya 'NotifyUserSessionRevoked' bildirimi başarıyla gönderildi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "'NotifyUserSessionRevoked' bildirimi gönderilirken hata oluştu.");
            }
        }

        private void RegisterAllHandlers()
        {
            foreach (var handler in _hubEventHandlers)
                handler.RegisterHandlers(_connection);
        }


        private async Task RegisterDeviceAfterConnection(CancellationToken cancellationToken)
        {
            try
            {
                var uniqueDeviceId = await _storageService.GetUniqueDeviceIdAsync();
                if (!string.IsNullOrEmpty(uniqueDeviceId))
                {
                    await _connection.InvokeAsync("RegisterActiveDevice", uniqueDeviceId, cancellationToken);
                    _logger.LogInformation("Aktif cihaz {DeviceId} olarak sunucuya kaydedildi.", uniqueDeviceId);
                }
                else
                {
                    _logger.LogWarning("UniqueDeviceId alınamadı, cihaz kaydedilemedi.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RegisterActiveDevice çağrılırken hata.");
            }
        }
    }
}
