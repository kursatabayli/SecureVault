using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Infrastructure.Helpers;
using SecureVault.App.Infrastructure.HttpHandlers;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin
{
    public class QrLoginOrchestratorService : IQrLoginOrchestrator
    {
        private readonly IInteractionService _interactionService;
        private readonly ICryptoService _cryptoService;
        private readonly IHttpHandlerPipelineBuilder _pipelineBuilder;
        private readonly IDispatcher _dispatcher;
        private HubConnection _hubConnection;
        private string _channelId;
        private ECDiffieHellman _ecdh;
        private byte[] _sharedSecret;
        private readonly string _hubUrl;

        private QrLoginRole _currentRole;
        private bool _isPublicKeySent = false;

        public event Func<string, Task> OnQrCodeAvailable;
        public event Func<string, Task> OnStatusUpdate;
        public event Func<LoginQrCodeDto, Task> OnLoginCredentialsReceived;
        public event Func<Task> OnAuthorizationComplete;
        public event Func<string, Task> OnError;

        public QrLoginOrchestratorService(IInteractionService interactionService, ICryptoService cryptoService, IHttpHandlerPipelineBuilder pipelineBuilder, IOptions<ApiSettings> apiSettings,
            IOptions<QrCodeSettings> qrCodeSettings, IDispatcher dispatcher)
        {
            _interactionService = interactionService;
            _cryptoService = cryptoService;
            _pipelineBuilder = pipelineBuilder;
            var baseUrl = new Uri(apiSettings.Value.BaseUrl);
            var fullHubUri = new Uri(baseUrl, qrCodeSettings.Value.HubPath);
            _hubUrl = fullHubUri.ToString();
            _dispatcher = dispatcher;
        }

        public async Task StartSession(QrLoginRole role, InitiationMethod method, string? scannedChannelId = null, CancellationToken cancellationToken = default)
        {
            await ResetStateAsync();
            _currentRole = role;

            try
            {
                await SafeInvokeAsync(() => OnStatusUpdate?.Invoke("Oturum başlatılıyor..."));

                if (method == InitiationMethod.GenerateQrCode)
                {
                    var response = await _interactionService.CreateQrLoginChannelAsync(cancellationToken);
                    if (string.IsNullOrEmpty(_channelId))
                        throw new InvalidOperationException("Sunucudan kanal ID'si alınamadı.");
                    _channelId = response.Value;
                    await SafeInvokeAsync(() => OnQrCodeAvailable?.Invoke(_channelId));
                }
                else
                {
                    if (string.IsNullOrEmpty(scannedChannelId))
                        throw new ArgumentNullException(nameof(scannedChannelId), "Taranan Channel ID boş olamaz.");
                    _channelId = scannedChannelId;
                }

                await ConnectToHubAndListen(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await DisposeAsync();
            }
            catch (Exception ex)
            {
                await SafeInvokeAsync(() => OnError?.Invoke($"Oturum başlatılamadı: {ex.Message}"));
                await DisposeAsync();
            }
        }

        private async Task ConnectToHubAndListen(CancellationToken cancellationToken)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    options.HttpMessageHandlerFactory = _ => _pipelineBuilder.CreatePipeline();
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On("StartKeyExchange", async () =>
            {
                await SafeInvokeAsync(() => OnStatusUpdate?.Invoke("Diğer cihaz doğrulandı. Güvenli kanal oluşturuluyor..."));
                await InitiateKeyExchange();
            });

            _hubConnection.On<string, string>("ReceiveMessage", async (type, payload) => await HandleReceivedMessage(type, payload, cancellationToken));
            _hubConnection.On<string>("Error", async (errorMessage) => await SafeInvokeAsync(() => OnError?.Invoke($"Sunucu hatası: {errorMessage}")));

            await _hubConnection.StartAsync(cancellationToken);
            await _hubConnection.InvokeAsync("JoinChannel", _channelId, cancellationToken);
            await SafeInvokeAsync(() => OnStatusUpdate?.Invoke("Diğer cihazın bağlanması veya QR kodu okutması bekleniyor..."));
        }

        private async Task InitiateKeyExchange(CancellationToken cancellationToken = default)
        {
            if (_isPublicKeySent) return;

            _ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var publicKey = _ecdh.PublicKey.ExportSubjectPublicKeyInfo();
            var publicKeyBase64 = Convert.ToBase64String(publicKey);

            await SendMessage("PublicKey", publicKeyBase64, cancellationToken);
            _isPublicKeySent = true;
        }

        private async Task HandleReceivedMessage(string messageType, string payload, CancellationToken cancellationToken = default)
        {
            if (messageType == "PublicKey")
            {
                await InitiateKeyExchange(cancellationToken);

                var counterPartyPublicKeyBytes = Convert.FromBase64String(payload);
                using var counterPartyEcdh = ECDiffieHellman.Create();
                counterPartyEcdh.ImportSubjectPublicKeyInfo(counterPartyPublicKeyBytes, out _);

                if (_ecdh == null)
                {
                    await SafeInvokeAsync(() => OnError?.Invoke("Kritik Hata: Anahtar değişimi sırasında anahtar çifti oluşturulamadı."));
                    return;
                }

                _sharedSecret = _ecdh.DeriveKeyMaterial(counterPartyEcdh.PublicKey);
                await SafeInvokeAsync(() => OnStatusUpdate?.Invoke("Güvenli kanal kuruldu."));
            }
            else if (messageType == "EncryptedLoginData" && _currentRole == QrLoginRole.Requester)
            {
                await SafeInvokeAsync(() => OnStatusUpdate?.Invoke("Oturum bilgileri alındı, doğrulanıyor..."));
                try
                {
                    var encryptedBytes = Convert.FromBase64String(payload);
                    var decryptedDto = _cryptoService.Decrypt<LoginQrCodeDto>(encryptedBytes, _sharedSecret);
                    await SafeInvokeAsync(() => OnLoginCredentialsReceived?.Invoke(decryptedDto));
                }
                catch (Exception ex)
                {
                    await SafeInvokeAsync(() => OnError?.Invoke($"Oturum bilgileri çözülemedi: {ex.Message}"));
                }
            }
        }

        public async Task SendCredentials(LoginQrCodeDto credentials)
        {
            if (_currentRole != QrLoginRole.Provider || _sharedSecret == null)
            {
                await SafeInvokeAsync(() => OnError?.Invoke("Bu cihaz oturum bilgisi gönderme yetkisine sahip değil veya güvenli kanal hazır değil."));
                return;
            }

            try
            {
                await SafeInvokeAsync(() => OnStatusUpdate?.Invoke("Oturum bilgileri şifrelenip gönderiliyor..."));
                var encryptedBytes = _cryptoService.Encrypt(credentials, _sharedSecret);
                var encryptedBase64 = Convert.ToBase64String(encryptedBytes);
                await SendMessage("EncryptedLoginData", encryptedBase64);
                await SafeInvokeAsync(() => OnAuthorizationComplete?.Invoke());
            }
            catch (Exception ex)
            {
                await SafeInvokeAsync(() => OnError?.Invoke($"Kimlik bilgileri gönderilemedi: {ex.Message}"));
            }
        }

        private async Task SendMessage(string messageType, string payload, CancellationToken cancellationToken = default)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
                await _hubConnection.InvokeAsync("SendMessageToChannel", _channelId, messageType, payload, cancellationToken);
        }

        private async Task ResetStateAsync()
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
            _ecdh?.Dispose();
            _ecdh = null;

            _isPublicKeySent = false;
            _sharedSecret = null;
            _channelId = null;
        }

        private async Task SafeInvokeAsync(Func<Task>? eventHandler)
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

        public async ValueTask DisposeAsync()
        {
            await ResetStateAsync();
        }
    }
}

