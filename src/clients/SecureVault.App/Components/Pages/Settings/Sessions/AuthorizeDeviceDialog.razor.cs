using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Application.Contracts.DTOs.Auth;
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
        private IDialogReference _qrCodeDialogReference;

        private string _statusMessage = "İşlem bekleniyor...";
        private bool _isProcessing = false;
        private CancellationTokenSource _sessionCts;
        private readonly DialogOptions DialogOptions = new() { BackdropClick = false, CloseOnEscapeKey = false, };

        protected override void OnInitialized()
        {
            Orchestrator.OnStatusUpdate += HandleStatusUpdate;
            Orchestrator.OnError += HandleError;
            Orchestrator.OnQrCodeAvailable += HandleQrCodeAvailable;
            Orchestrator.OnAuthorizationComplete += HandleAuthorizationComplete;
        }

        private async Task StartByScanning()
        {
            _isProcessing = true;

            var dialog = await DialogService.ShowAsync<ScanQRCodeDialog>("QR Kodu Tara", DialogOptions);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is string scannedChannelId && !string.IsNullOrEmpty(scannedChannelId))
            {
                try
                {
                    _sessionCts = new CancellationTokenSource();
                    await Orchestrator.StartSession(QrLoginRole.Provider, InitiationMethod.ScanQrCode, scannedChannelId, _sessionCts.Token);
                }
                catch (OperationCanceledException)
                {
                    _statusMessage = "İşlem iptal edildi.";
                    _isProcessing = false;
                    await InvokeAsync(StateHasChanged);
                }
                catch (Exception ex)
                {
                    await HandleError($"İşlem başlatılamadı: {ex.Message}");
                }
            }
            else
            {
                _isProcessing = false;
                StateHasChanged();
            }
        }

        private async Task StartByGenerating()
        {
            _isProcessing = true;
            try
            {
                _sessionCts = new CancellationTokenSource();

                await Orchestrator.StartSession(QrLoginRole.Provider, InitiationMethod.GenerateQrCode, null, _sessionCts.Token);
            }
            catch (OperationCanceledException)
            {
                _statusMessage = "İşlem iptal edildi.";
                _isProcessing = false;
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                await HandleError($"İşlem başlatılamadı: {ex.Message}");
            }
        }

        private async Task HandleStatusUpdate(string status)
        {
            _statusMessage = status;
            if (status.Contains("Diğer cihaz doğrulandı") || status.Contains("Güvenli kanal oluşturuluyor"))
                _qrCodeDialogReference?.Close();

            if (status == "Güvenli kanal kuruldu.")
            {
                try
                {
                    var credentials = new LoginQrCodeDto
                    {
                        Email = StorageService.GetEmail(),
                        PrivateKey = await StorageService.GetPrivateKeyAsByteAsync(),
                        EncryptionKey = await StorageService.GetEncryptionKeyAsByteAsync()
                    };

                    await Orchestrator.SendCredentials(credentials);
                }
                catch (Exception ex)
                {
                    await HandleError($"Oturum bilgileri alınırken bir hata oluştu: {ex.Message}");
                }
            }
            await InvokeAsync(StateHasChanged);

        }

        private async Task HandleError(string error)
        {
            Snackbar.Add(error, Severity.Error, config => { config.CloseAfterNavigation = true; });
            _isProcessing = false;
             _qrCodeDialogReference?.Close();
            MudDialog.Close(DialogResult.Cancel());
        }

        private async Task HandleQrCodeAvailable(string channelId)
        {
            var parameters = new DialogParameters<DisplayQrCodeDialog> { { x => x.ChannelId, channelId } };
            _qrCodeDialogReference = await DialogService.ShowAsync<DisplayQrCodeDialog>("Diğer Cihazla Okutun", parameters, DialogOptions);
            var result = await _qrCodeDialogReference.Result;
            if (result.Canceled)
                Cancel();
        }

        private async Task HandleAuthorizationComplete()
        {
            Snackbar.Add("Yeni cihaz başarıyla yetkilendirildi!", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }

        private void Cancel() => MudDialog.Cancel();

        public async ValueTask DisposeAsync()
        {
            Orchestrator.OnStatusUpdate -= HandleStatusUpdate;
            Orchestrator.OnError -= HandleError;
            Orchestrator.OnQrCodeAvailable -= HandleQrCodeAvailable;
            Orchestrator.OnAuthorizationComplete -= HandleAuthorizationComplete;
            _sessionCts?.Cancel();
            _sessionCts?.Dispose();
            await Orchestrator.DisposeAsync();
        }
    }
}
