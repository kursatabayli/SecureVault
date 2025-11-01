using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Device;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Contracts.DTOs.Session;
using System.Text.Json;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin
{
    public class QrLoginOrchestratorService : IQrLoginOrchestrator, IQrLoginContext
    {
        // --- Bağımlılıklar ---
        private readonly IInteractionService _interactionService;
        private readonly IQrHubConnection _hubConnection;
        private readonly ISecureChannelManager _secureChannelManager;
        private readonly IDispatcher _dispatcher;
        private readonly IDeviceInfoService _deviceInfoService;
        private readonly ILogger<QrLoginOrchestratorService> _logger;
        private readonly IReadOnlyDictionary<string, IMessageHandler> _messageHandlers;

        public QrLoginRole CurrentRole => _currentRole;
        public bool IsPublicKeySent => _isPublicKeySent;
        public ISecureChannelManager SecureChannelManager => _secureChannelManager;

        // --- Durum (State) Değişkenleri ---
        private string _channelId;
        private QrLoginRole _currentRole;
        private QrSessionState _currentState = QrSessionState.Idle;
        private bool _isPublicKeySent = false;

        // --- Olaylar (Events) ---
        public event Func<QrSessionState, string, Task> OnStateChanged;
        public event Func<string, Task> OnQrCodeAvailable;
        public event Func<LoginQrCodeDto, Task> OnLoginCredentialsReceived;
        public event Func<Task> OnAuthorizationComplete;
        public event Func<DeviceDetailDto, Task> OnDeviceAuthorizationRequired;

        public QrLoginOrchestratorService(
            IInteractionService interactionService,
            IQrHubConnection hubConnection,
            ISecureChannelManager secureChannelManager,
            IDispatcher dispatcher,
            IDeviceInfoService deviceInfoService,
            ILogger<QrLoginOrchestratorService> logger,
            IEnumerable<IMessageHandler> handlers)
        {
            _interactionService = interactionService;
            _hubConnection = hubConnection;
            _secureChannelManager = secureChannelManager;
            _dispatcher = dispatcher;
            _deviceInfoService = deviceInfoService;
            _logger = logger;
            _messageHandlers = handlers.ToDictionary(h => h.MessageType, h => h);

            // Olayları dinlemeye başla
            _hubConnection.OnMessageReceived += HandleReceivedMessage;
            _hubConnection.OnErrorReceived += HandleErrorReceived;
        }

        public async Task SetState(QrSessionState newState, string message)
        {
            _currentState = newState;
            _logger.LogInformation("State changed to {State}: {Message}", newState, message);
            await SafeInvokeAsync(() => OnStateChanged?.Invoke(newState, message));
        }

        public async Task StartSession(QrLoginRole role, CancellationToken cancellationToken = default)
        {
            await ResetStateAsync();
            _currentRole = role;

            try
            {
                await SetState(QrSessionState.CreatingChannel, "Oturum kanalı oluşturuluyor...");
                var result = await _interactionService.CreateQrLoginChannelAsync(cancellationToken);
                _channelId = result.Value;
                if (string.IsNullOrEmpty(_channelId))
                    throw new InvalidOperationException("Sunucudan kanal ID'si alınamadı.");

                await SafeInvokeAsync(() => OnQrCodeAvailable?.Invoke(_channelId));
                await _hubConnection.ConnectAndJoinChannelAsync(_channelId, cancellationToken);
                await SetState(QrSessionState.AwaitingPeer, "Diğer cihazın bağlanması bekleniyor...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oturum başlatılamadı.");
                await SetState(QrSessionState.Error, $"Oturum başlatılamadı: {ex.Message}");
                await DisposeAsync();
            }
        }

        public async Task JoinSessionByScanning(QrLoginRole role, string scannedChannelId, CancellationToken cancellationToken = default)
        {
            await ResetStateAsync();
            _currentRole = role;
            _channelId = scannedChannelId;

            try
            {
                if (string.IsNullOrEmpty(scannedChannelId))
                    throw new ArgumentNullException(nameof(scannedChannelId), "Taranan Channel ID boş olamaz.");

                await _hubConnection.ConnectAndJoinChannelAsync(_channelId, cancellationToken);
                await SetState(QrSessionState.AwaitingPeer, "Diğer cihazın bağlanması bekleniyor...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oturuma katılım sağlanamadı.");
                await SetState(QrSessionState.Error, $"Oturuma katılım sağlanamadı: {ex.Message}");
                await DisposeAsync();
            }
        }
        private async Task HandleReceivedMessage(string messageType, string payload)
        {
            if (_messageHandlers.TryGetValue(messageType, out var handler))
            {
                await handler.HandleAsync(this, payload);
            }
            else
            {
                _logger.LogWarning("Unknown message type received: {MessageType}", messageType);
            }
        }

        private async Task HandleErrorReceived(string errorMessage)
        {
            await SetState(QrSessionState.Error, $"Sunucu hatası: {errorMessage}");
        }

        public async Task InitiateKeyExchange()
        {
            if (_isPublicKeySent) return;

            await SetState(QrSessionState.ExchangingKeys, "Anahtar değişimi yapılıyor...");
            var publicKeyBase64 = _secureChannelManager.InitiateKeyExchange();
            await _hubConnection.SendMessageAsync("PublicKey", publicKeyBase64);
            _isPublicKeySent = true;
        }

        public async Task SendDeviceInfo()
        {
            var deviceInfo = new DeviceDetailDto
            {
                UniqueDeviceId = await _deviceInfoService.GetUniqueDeviceIdAsync(),
                DeviceModel = _deviceInfoService.GetDeviceModel(),
                DeviceName = _deviceInfoService.GetDeviceName(),
                DeviceManufacturer = _deviceInfoService.GetDeviceManufacturer(),
                OperatingSystem = _deviceInfoService.GetOperatingSystemInfo()
            };

            var payload = JsonSerializer.Serialize(deviceInfo);
            await _hubConnection.SendMessageAsync("DeviceInfoRequest", payload);
        }

        public async Task ApproveAuthorization()
        {
            await _hubConnection.SendMessageAsync("AuthorizationApproved", string.Empty);
            await InitiateKeyExchange();
        }

        public async Task DenyAuthorization()
        {
            await _hubConnection.SendMessageAsync("AuthorizationDenied", string.Empty);
            await SetState(QrSessionState.Idle, "Bağlantı isteği reddedildi.");
            await DisposeAsync();
        }

        public async Task SendCredentials(LoginQrCodeDto credentials)
        {
            if (_currentRole != QrLoginRole.Provider || !_secureChannelManager.IsSecureChannelEstablished)
            {
                await SetState(QrSessionState.Error, "Bu cihaz oturum bilgisi gönderme yetkisine sahip değil veya güvenli kanal hazır değil.");
                return;
            }

            try
            {
                await SetState(QrSessionState.TransferringCredentials, "Oturum bilgileri şifrelenip gönderiliyor...");
                var encryptedBytes = _secureChannelManager.Encrypt(credentials);
                var encryptedBase64 = Convert.ToBase64String(encryptedBytes);
                await _hubConnection.SendMessageAsync("EncryptedLoginData", encryptedBase64);
                await SafeInvokeAsync(() => OnAuthorizationComplete?.Invoke());
                await SetState(QrSessionState.Completed, "Yetkilendirme tamamlandı.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kimlik bilgileri gönderilemedi.");
                await SetState(QrSessionState.Error, $"Kimlik bilgileri gönderilemedi: {ex.Message}");
            }
        }

        public async Task RaiseDeviceAuthorizationRequired(DeviceDetailDto deviceInfo) => await SafeInvokeAsync(() => OnDeviceAuthorizationRequired?.Invoke(deviceInfo));
        public async Task RaiseLoginCredentialsReceived(LoginQrCodeDto credentials) => await SafeInvokeAsync(() => OnLoginCredentialsReceived?.Invoke(credentials));

        public async Task SafeInvokeAsync(Func<Task>? eventHandler)
        {
            if (eventHandler == null) return;

            var handlers = eventHandler.GetInvocationList();
            foreach (var handler in handlers)
            {
                await _dispatcher.DispatchAsync(async () =>
                {
                    try
                    {
                        if (handler is Func<Task> taskHandler)
                        {
                            await taskHandler();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in event subscriber: {ex}");
                    }
                });
            }
        }
        private Task ResetStateAsync()
        {
            _logger.LogInformation("Resetting QR login session state.");
            _isPublicKeySent = false;
            _channelId = null;
            _currentState = QrSessionState.Idle;

            return Task.CompletedTask;
        }
        public ValueTask DisposeAsync()
        {
            return new ValueTask(ResetStateAsync());
        }
    }
}

