using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
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
        private readonly IStorageService _storageService;
        private readonly IActiveSessionTracker _activeSessionTracker;
        private HubConnection _connection;
        private readonly string _hubUrl;

        public SyncConnectionService(ILogger<SyncConnectionService> logger, IBackgroundSyncService backgroundSyncService, IHttpHandlerPipelineBuilder pipelineBuilder, IOptions<ApiSettings> apiSettings,
            IOptions<SignalRSettings> signalRSettings, IStorageService storageService, IActiveSessionTracker activeSessionTracker)
        {
            _logger = logger;
            _backgroundSyncService = backgroundSyncService;
            _pipelineBuilder = pipelineBuilder;
            _storageService = storageService;
            _activeSessionTracker = activeSessionTracker;
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
                await _backgroundSyncService.SynchronizeAsync(CancellationToken.None);
            });

            _connection.On<List<string>>("ActiveDevicesUpdated", (deviceIds) =>
            {
                _logger.LogInformation("Aktif cihaz listesi güncellendi. {Count} cihaz aktif.", deviceIds.Count);
                (_activeSessionTracker as ActiveSessionTracker)?.UpdateActiveDevices(deviceIds);
            });

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
