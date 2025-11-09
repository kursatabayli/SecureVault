using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Contracts.DTOs.Session;
using SecureVault.App.Components.Helpers;

namespace SecureVault.App.Components.Pages.Settings.Sessions
{
    public partial class AuthorizeDeviceDialog : ComponentBase, IAsyncDisposable
    {
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private IQrLoginOrchestrator Orchestrator { get; set; }
        [Inject] private IStorageService StorageService { get; set; }

        private string _statusMessage = "İşlem bekleniyor...";
        private CancellationTokenSource _sessionCts;
        private readonly DialogOptions DialogOptions = new() { BackdropClick = false, CloseOnEscapeKey = false, };
        private DeviceDetailDto _deviceToConfirm;
        private bool _rememberMeDecision = false;
        protected override void OnInitialized()
        {
            Orchestrator.OnStateChanged += HandleStateChanged;
            Orchestrator.OnQrCodeAvailable += HandleQrCodeAvailable;
            Orchestrator.OnAuthorizationComplete += HandleAuthorizationComplete;
            Orchestrator.OnDeviceAuthorizationRequired += HandleDeviceAuthorizationRequired;
        }
        private void SetView(DialogView view, string statusMessage = null, string title = null, string icon = null)
        {
            _currentView = view;
            if (statusMessage != null) _statusMessage = statusMessage;
            if (title != null) _titleText = title;
            if (icon != null) _titleIcon = icon;
            InvokeAsync(StateHasChanged);
        }
        private Task HandleDeviceAuthorizationRequired(DeviceDetailDto deviceInfo)
        {
            _channelIdForQr = null;
            _deviceToConfirm = deviceInfo;
            SetView(DialogView.AwaitingConfirmation, title: "Yeni Cihaz Bağlantı İsteği");
            return Task.CompletedTask;
        }
        private async Task HandleConfirmationDecision((bool IsApproved, bool RememberMe) decision)
        {
            if (decision.IsApproved)
            {
                _rememberMeDecision = decision.RememberMe;
                SetView(DialogView.Processing, "Cihaz onaylanıyor...");
                await Orchestrator.ApproveAuthorization();
            }
            else
            {
                SetView(DialogView.Processing, "Cihaz reddediliyor...");
                await Orchestrator.DenyAuthorization();
                SetView(DialogView.InitialSelection, title: "Yeni Cihazı Yetkilendir", icon: Icons.Material.Filled.PhonelinkSetup);
            }
        }
        private async Task ScanQrCode()
        {
            SetView(DialogView.Processing, "QR kod taranıyor...");

            var dialog = await DialogService.ShowAsync<ScanQRCodeDialog>("QR Kodu Tara", DialogOptions);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is string scannedChannelId && !string.IsNullOrEmpty(scannedChannelId))
            {
                try
                {
                    _sessionCts = new CancellationTokenSource();
                    await Orchestrator.JoinSessionByScanning(QrLoginRole.Provider, scannedChannelId, _sessionCts.Token);
                }
                catch (OperationCanceledException)
                {
                    await InvokeAsync(StateHasChanged);
                }
                catch (Exception ex)
                {
                    await HandleStateChanged(QrSessionState.Error, $"Oturuma katılım sağlanamadı: {ex.Message}");
                }
            }
            else
            {
                SetView(DialogView.InitialSelection);
            }
        }

        private async Task GenerateQrCode()
        {
            SetView(DialogView.Processing, "Güvenli oturum başlatılıyor...");
            try
            {
                _sessionCts = new CancellationTokenSource();

                await Orchestrator.StartSession(QrLoginRole.Provider, _sessionCts.Token);
            }
            catch (OperationCanceledException)
            {
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                await HandleStateChanged(QrSessionState.Error, $"İşlem başlatılamadı: {ex.Message}");
            }
        }

        private async Task HandleStateChanged(QrSessionState state, string message)
        {
            _statusMessage = message;

            switch (state)
            {
                case QrSessionState.PeerJoined:
                    if (_currentView == DialogView.DisplayingQr)
                        _channelIdForQr = null;
                    SetView(DialogView.Processing, message);
                    break;
                case QrSessionState.ExchangingKeys:
                    if (_currentView == DialogView.DisplayingQr)
                        _channelIdForQr = null;
                    SetView(DialogView.Processing, message);
                    break;

                case QrSessionState.SecureChannelEstablished:
                    SetView(DialogView.Processing, message);
                    await SendCredentialsSafe();
                    break;

                case QrSessionState.Error:
                    Snackbar.Add(message, Severity.Error, config => { config.CloseAfterNavigation = true; });
                    _sessionCts?.Cancel();
                    MudDialog.Close(DialogResult.Cancel());
                    break;
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task SendCredentialsSafe()
        {
            try
            {
                var credentials = new LoginQrCodeDto
                {
                    Email = StorageService.GetEmail(),
                    PrivateKey = await StorageService.GetPrivateKeyAsByteAsync(),
                    EncryptionKey = await StorageService.GetEncryptionKeyAsByteAsync(),
                    RememberMe = _rememberMeDecision
                };

                await Orchestrator.SendCredentials(credentials);
            }
            catch (Exception ex)
            {
                await HandleStateChanged(QrSessionState.Error, $"Oturum bilgileri alınırken bir hata oluştu: {ex.Message}");
            }
        }

        private Task HandleQrCodeAvailable(string channelId)
        {
            _channelIdForQr = channelId;
            SetView(DialogView.DisplayingQr, title: "Diğer Cihazla Okutun", icon: Icons.Material.Filled.QrCode2);
            return Task.CompletedTask;
        }
        private async Task HandleQrClose()
        {
            _sessionCts?.Cancel();
            await Orchestrator.DisposeAsync();
            SetView(DialogView.InitialSelection, title: "Yeni Cihazı Yetkilendir", icon: Icons.Material.Filled.PhonelinkSetup);
        }
        private async Task HandleAuthorizationComplete()
        {
            Snackbar.Add("Yeni cihaz başarıyla yetkilendirildi!", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }

        private async Task Cancel()
        {
            await Orchestrator.DisposeAsync();
            MudDialog.Cancel();
        }

        public async ValueTask DisposeAsync()
        {
            Orchestrator.OnStateChanged -= HandleStateChanged;
            Orchestrator.OnQrCodeAvailable -= HandleQrCodeAvailable;
            Orchestrator.OnAuthorizationComplete -= HandleAuthorizationComplete;
            Orchestrator.OnDeviceAuthorizationRequired -= HandleDeviceAuthorizationRequired;
            _sessionCts?.Cancel();
            _sessionCts?.Dispose();
            await Orchestrator.DisposeAsync();
        }
    }
}
