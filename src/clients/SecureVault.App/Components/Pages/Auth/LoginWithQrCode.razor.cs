using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Features.CQRS.Auth.Commands;
using SecureVault.App.Components.Helpers;

namespace SecureVault.App.Components.Pages.Auth
{
    public partial class LoginWithQrCode : ComponentBase, IAsyncDisposable
    {
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IMediator Mediator { get; set; }
        [Inject] private IMapper Mapper { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private IQrLoginOrchestrator Orchestrator { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
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
            Orchestrator.OnLoginCredentialsReceived += HandleLoginCredentialsReceived;
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
                    await Orchestrator.StartSession(QrLoginRole.Requester, InitiationMethod.ScanQrCode, scannedChannelId, _sessionCts.Token);
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

                await Orchestrator.StartSession(QrLoginRole.Requester, InitiationMethod.GenerateQrCode, null, _sessionCts.Token);
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
            await InvokeAsync(StateHasChanged);
        }
        private async Task HandleError(string error)
        {
            Snackbar.Add(error, Severity.Error);
            _isProcessing = false;
            _qrCodeDialogReference?.Close();
            await InvokeAsync(StateHasChanged);
        }
        private async Task HandleQrCodeAvailable(string channelId)
        {
            var parameters = new DialogParameters<DisplayQrCodeDialog> { { x => x.ChannelId, channelId } };
            _qrCodeDialogReference = await DialogService.ShowAsync<DisplayQrCodeDialog>("Giriş Yapmak İçin Okutun", parameters, DialogOptions);
        }

        private async Task HandleLoginCredentialsReceived(LoginQrCodeDto credentials)
        {
            _statusMessage = "Oturum bilgileri doğrulandı. Giriş yapılıyor...";
            await InvokeAsync(StateHasChanged);

            var command = Mapper.Map<LoginWithQrCodeCommand>(credentials);
            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                NavigationManager.NavigateTo("/", forceLoad: true);
            }
            else
            {
                await HandleError(result.Error.Message ?? "Giriş işlemi sırasında bilinmeyen bir hata oluştu.");
            }
        }

        private void GoBack()
        {
            _qrCodeDialogReference?.Close();
            NavigationManager.NavigateTo("/login");
        }
        public async ValueTask DisposeAsync()
        {
            Orchestrator.OnStatusUpdate -= HandleStatusUpdate;
            Orchestrator.OnError -= HandleError;
            Orchestrator.OnQrCodeAvailable -= HandleQrCodeAvailable;
            Orchestrator.OnLoginCredentialsReceived -= HandleLoginCredentialsReceived;
            _sessionCts?.Cancel();
            _sessionCts?.Dispose();
            await Orchestrator.DisposeAsync();
        }
    }
}
