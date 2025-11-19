using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Contracts.DTOs.Session;
using SecureVault.App.Components.Helpers;

namespace SecureVault.App.Components.Pages.Settings.Sessions;

public partial class AuthorizeDeviceDialog : ComponentBase, IAsyncDisposable
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    [Inject] private ISnackbar Snackbar { get; set; }
    [Inject] private IDialogService DialogService { get; set; }
    [Inject] private IQrLoginOrchestratorFactory OrchestratorFactory { get; set; }

    private IQrLoginOrchestrator _orchestrator;
    private string _statusMessage = "İşlem bekleniyor...";
    private CancellationTokenSource _sessionCts;
    private readonly DialogOptions DialogOptions = new() { BackdropClick = false, CloseOnEscapeKey = false, };
    private DeviceDetailDto _deviceToConfirm;
    private bool _rememberMeDecision = false;

    private async Task InitializeOrchestrator()
    {
        await DisposeCurrentOrchestrator(false);
        _orchestrator = OrchestratorFactory.Create();
        _orchestrator.OnStateChanged += HandleStateChanged;
        _orchestrator.OnQrCodeAvailable += HandleQrCodeAvailable;
        _orchestrator.OnAuthorizationComplete += HandleAuthorizationComplete;
        _orchestrator.OnDeviceAuthorizationRequired += HandleDeviceAuthorizationRequired;
        _sessionCts?.Dispose();
        _sessionCts = new CancellationTokenSource();
    }

    private async Task SetView(DialogView view, string statusMessage = null, string title = null, string icon = null)
    {
        _currentView = view;
        if (statusMessage != null) _statusMessage = statusMessage;
        if (title != null) _titleText = title;
        if (icon != null) _titleIcon = icon;
        await InvokeAsync(StateHasChanged);
    }
    private async Task HandleDeviceAuthorizationRequired(DeviceDetailDto deviceInfo)
    {
        _channelIdForQr = null;
        _deviceToConfirm = deviceInfo;
        await SetView(DialogView.AwaitingConfirmation, title: "Yeni Cihaz Bağlantı İsteği");
    }
    private async Task HandleConfirmationDecision((bool IsApproved, bool RememberMe) decision)
    {
        if (decision.IsApproved)
        {
            _rememberMeDecision = decision.RememberMe;
            await SetView(DialogView.Processing, "Cihaz onaylanıyor...");
            await _orchestrator.ApproveAuthorization();
        }
        else
        {
            await SetView(DialogView.Processing, "Cihaz reddediliyor...");
            await _orchestrator.DenyAuthorization();
            await SetView(DialogView.InitialSelection, title: "Yeni Cihazı Yetkilendir", icon: Icons.Material.Filled.PhonelinkSetup);
        }
    }
    private async Task ScanQrCode()
    {
        await SetView(DialogView.Processing, "QR kod taranıyor...");

        await InitializeOrchestrator();

        var dialog = await DialogService.ShowAsync<ScanQRCodeDialog>("QR Kodu Tara", DialogOptions);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is string scannedChannelId && !string.IsNullOrEmpty(scannedChannelId))
        {
            try
            {
                await _orchestrator.JoinSessionByScanning(QrLoginRole.Provider, scannedChannelId, _sessionCts.Token);
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
            await SetView(DialogView.InitialSelection);
        }
    }

    private async Task GenerateQrCode()
    {
        await SetView(DialogView.Processing, "Güvenli oturum başlatılıyor...");

        await InitializeOrchestrator();

        try
        {
            await _orchestrator.StartSession(QrLoginRole.Provider, _sessionCts.Token);
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
                await SetView(DialogView.Processing, message);
                break;
            case QrSessionState.ExchangingKeys:
                if (_currentView == DialogView.DisplayingQr)
                    _channelIdForQr = null;
                await SetView(DialogView.Processing, message);
                break;

            case QrSessionState.SecureChannelEstablished:
                await SetView(DialogView.Processing, message);
                await SendCredentialsSafe();
                break;

            case QrSessionState.Error:
                Snackbar.Add(message, Severity.Error, config => { config.CloseAfterNavigation = true; });
                MudDialog.Close(DialogResult.Cancel());
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SendCredentialsSafe()
    {
        try
        {
            await _orchestrator.SendCredentials(_rememberMeDecision);
        }
        catch (Exception ex)
        {
            await HandleStateChanged(QrSessionState.Error, $"Oturum bilgileri alınırken bir hata oluştu: {ex.Message}");
        }
    }

    private async Task HandleQrCodeAvailable(string channelId)
    {
        _channelIdForQr = channelId;
        await SetView(DialogView.DisplayingQr, title: "Diğer Cihazla Okutun", icon: Icons.Material.Filled.QrCode2);
    }
    private async Task HandleQrClose()
    {
        await DisposeCurrentOrchestrator(false);

        await SetView(DialogView.InitialSelection, title: "Yeni Cihazı Yetkilendir", icon: Icons.Material.Filled.PhonelinkSetup);
    }
    private async Task HandleAuthorizationComplete()
    {
        Snackbar.Add("Yeni cihaz başarıyla yetkilendirildi!", Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
    }

    private async Task Cancel()
    {
        MudDialog.Cancel();
    }
    private async Task DisposeCurrentOrchestrator(bool cancelDialog)
    {
        _sessionCts?.Cancel();
        _sessionCts?.Dispose();
        _sessionCts = null;

        if (_orchestrator != null)
        {
            _orchestrator.OnStateChanged -= HandleStateChanged;
            _orchestrator.OnQrCodeAvailable -= HandleQrCodeAvailable;
            _orchestrator.OnAuthorizationComplete -= HandleAuthorizationComplete;
            _orchestrator.OnDeviceAuthorizationRequired -= HandleDeviceAuthorizationRequired;

            await _orchestrator.DisposeAsync();
            _orchestrator = null;
        }

        if (cancelDialog)
        {
            MudDialog.Close(DialogResult.Cancel());
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeCurrentOrchestrator(false);
    }
}
