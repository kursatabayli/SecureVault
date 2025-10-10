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

        private CancellationTokenSource _sessionCts;
        private readonly DialogOptions DialogOptions = new() { BackdropClick = false, CloseOnEscapeKey = false, };
        protected override void OnInitialized()
        {
            Orchestrator.OnStateChanged += HandleStateChanged;
            Orchestrator.OnQrCodeAvailable += HandleQrCodeAvailable;
            Orchestrator.OnLoginCredentialsReceived += HandleLoginCredentialsReceived;
        }
        private void SetView(LoginPageView view, string statusMessage = null)
        {
            _currentView = view;
            if (statusMessage is not null)
                _statusMessage = statusMessage;
            InvokeAsync(StateHasChanged);
        }
        private async Task ScanQrCode()
        {
            SetView(LoginPageView.Processing, "QR kod taranıyor...");

            var dialog = await DialogService.ShowAsync<ScanQRCodeDialog>("QR Kodu Tara", DialogOptions);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is string scannedChannelId && !string.IsNullOrEmpty(scannedChannelId))
            {
                try
                {
                    _sessionCts = new CancellationTokenSource();
                    await Orchestrator.JoinSessionByScanning(QrLoginRole.Requester, scannedChannelId, _sessionCts.Token);
                }
                catch (OperationCanceledException)
                {
                    _statusMessage = "İşlem iptal edildi.";
                    await InvokeAsync(StateHasChanged);
                }
                catch (Exception ex)
                {
                    await HandleStateChanged(QrSessionState.Error, $"Oturuma katılım sağlanamadı: {ex.Message}");
                }
            }
            else
                SetView(LoginPageView.InitialSelection);

        }

        private async Task GenerateQrCode()
        {
            SetView(LoginPageView.Processing, "Güvenli oturum başlatılıyor...");
            try
            {
                _sessionCts = new CancellationTokenSource();

                await Orchestrator.StartSession(QrLoginRole.Requester, _sessionCts.Token);
            }
            catch (OperationCanceledException)
            {
                _statusMessage = "İşlem iptal edildi.";
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
                case QrSessionState.AwaitingAuthorization:
                    if (_currentView == LoginPageView.DisplayingQr)
                        _channelIdForQr = null;
                    SetView(LoginPageView.Processing, message);
                    break;

                case QrSessionState.Error:
                    Snackbar.Add(message, Severity.Error);
                    SetView(LoginPageView.InitialSelection);
                    break;
            }
            await InvokeAsync(StateHasChanged);
        }
        private Task HandleQrCodeAvailable(string channelId)
        {
            _channelIdForQr = channelId;
            SetView(LoginPageView.DisplayingQr);
            return Task.CompletedTask;
        }
        private void HandleQrClose()
        {
            _sessionCts?.Cancel();
            SetView(LoginPageView.InitialSelection);
        }
        private async Task HandleLoginCredentialsReceived(LoginQrCodeDto credentials)
        {
            SetView(LoginPageView.Processing, "Oturum bilgileri doğrulandı. Giriş yapılıyor...");

            var command = Mapper.Map<LoginWithQrCodeCommand>(credentials);
            var result = await Mediator.Send(command);

            if (result.IsSuccess)
                NavigationManager.NavigateTo("/", forceLoad: true);
            else
                await HandleStateChanged(QrSessionState.Error, result.Error.Message ?? "Giriş işlemi sırasında bilinmeyen bir hata oluştu.");
        }

        private void GoBack()
        {
            _sessionCts?.Cancel();
            NavigationManager.NavigateTo("/login");
        }
        public async ValueTask DisposeAsync()
        {
            Orchestrator.OnStateChanged -= HandleStateChanged;
            Orchestrator.OnQrCodeAvailable -= HandleQrCodeAvailable;
            Orchestrator.OnLoginCredentialsReceived -= HandleLoginCredentialsReceived;
            _sessionCts?.Cancel();
            _sessionCts?.Dispose();
            await Orchestrator.DisposeAsync();
        }
    }
}
