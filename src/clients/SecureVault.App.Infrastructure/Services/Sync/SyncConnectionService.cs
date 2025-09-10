using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Infrastructure.Helpers;
using SecureVault.App.Infrastructure.HttpHandlers;

namespace SecureVault.App.Infrastructure.Services.Sync
{
    public class SyncConnectionService : ISyncConnectionService
    {
        private readonly ILogger<SyncConnectionService> _logger;
        private readonly IBackgroundSyncService _backgroundSyncService;
        private readonly IHttpHandlerPipelineBuilder _pipelineBuilder;
        private HubConnection _connection;
        private readonly string _hubUrl;

        public SyncConnectionService(ILogger<SyncConnectionService> logger, IBackgroundSyncService backgroundSyncService, IHttpHandlerPipelineBuilder pipelineBuilder, IOptions<ApiSettings> apiSettings,
            IOptions<SignalRSettings> signalRSettings)
        {
            _logger = logger;
            _backgroundSyncService = backgroundSyncService;
            _pipelineBuilder = pipelineBuilder;
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

            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    options.HttpMessageHandlerFactory = _ => _pipelineBuilder.CreatePipeline();
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On("SyncRequired", async () =>
            {
                _logger.LogInformation("Sunucudan 'SyncRequired' bildirimi alındı. Catch-Up Sync tetikleniyor.");
                await _backgroundSyncService.SynchronizeAsync(cancellationToken);
            });

            try
            {
                _logger.LogInformation("SignalR Hub'ına bağlanılıyor...");
                await _connection.StartAsync(cancellationToken);
                _logger.LogInformation("SignalR Hub'ına başarıyla bağlanıldı. ConnectionId: {ConnectionId}", _connection.ConnectionId);
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
    }
}
