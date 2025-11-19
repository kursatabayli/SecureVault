using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Features.CQRS.Auth.Commands;
using SecureVault.App.Components.Helpers;

namespace SecureVault.App.Components.Pages.Auth;

public partial class LoginWithQrCode : ComponentBase, IAsyncDisposable
{
    [Inject] private ISnackbar Snackbar { get; set; }
    [Inject] private IMediator Mediator { get; set; }
    [Inject] private IMapper Mapper { get; set; }
    [Inject] private IDialogService DialogService { get; set; }
    [Inject] private IQrLoginOrchestratorFactory OrchestratorFactory { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }

    private IQrLoginOrchestrator _orchestrator;
    private CancellationTokenSource _sessionCts;
    private readonly DialogOptions DialogOptions = new() { BackdropClick = false, CloseOnEscapeKey = false, };

    private async Task SetView(LoginPageView view, string statusMessage = null)
    {
        _currentView = view;
        if (statusMessage is not null)
            _statusMessage = statusMessage;
        await InvokeAsync(StateHasChanged);
    }
    private async Task ScanQrCode()
    {
        await SetView(LoginPageView.Processing, "QR kod taranıyor...");

        await InitializeOrchestrator();

        var dialog = await DialogService.ShowAsync<ScanQRCodeDialog>("QR Kodu Tara", DialogOptions);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is string scannedChannelId && !string.IsNullOrEmpty(scannedChannelId))
        {
            try
            {
                await _orchestrator.JoinSessionByScanning(QrLoginRole.Requester, scannedChannelId, _sessionCts.Token);
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
            await SetView(LoginPageView.InitialSelection);

    }

    private async Task GenerateQrCode()
    {
        await SetView(LoginPageView.Processing, "Güvenli oturum başlatılıyor...");

        await InitializeOrchestrator();

        try
        {
            await _orchestrator.StartSession(QrLoginRole.Requester, _sessionCts.Token);
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
                await SetView(LoginPageView.Processing, message);
                break;

            case QrSessionState.Error:
                Snackbar.Add(message, Severity.Error);
                await SetView(LoginPageView.InitialSelection);
                break;
        }
        await InvokeAsync(StateHasChanged);
    }
    private async Task HandleQrCodeAvailable(string channelId)
    {
        _channelIdForQr = channelId;
        await SetView(LoginPageView.DisplayingQr);
    }
    private async Task HandleQrClose()
    {
        await DisposeCurrentOrchestrator();

        await SetView(LoginPageView.InitialSelection);
    }
    private async Task HandleLoginCredentialsReceived(LoginQrCodeDto credentials)
    {
        await SetView(LoginPageView.Processing, "Oturum bilgileri doğrulandı. Giriş yapılıyor...");

        var command = Mapper.Map<LoginWithQrCodeCommand>(credentials);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
            NavigationManager.NavigateTo("/");
        else
            await HandleStateChanged(QrSessionState.Error, result.Error.Message ?? "Giriş işlemi sırasında bilinmeyen bir hata oluştu.");
    }

    private async Task GoBack()
    {
        NavigationManager.NavigateTo("/login");
    }
    private async Task InitializeOrchestrator()
    {
        await DisposeCurrentOrchestrator();

        _orchestrator = OrchestratorFactory.Create();

        _orchestrator.OnStateChanged += HandleStateChanged;
        _orchestrator.OnQrCodeAvailable += HandleQrCodeAvailable;
        _orchestrator.OnLoginCredentialsReceived += HandleLoginCredentialsReceived;

        _sessionCts?.Dispose();
        _sessionCts = new CancellationTokenSource();
    }
    private async Task DisposeCurrentOrchestrator()
    {
        _sessionCts?.Cancel();
        _sessionCts?.Dispose();
        _sessionCts = null;

        if (_orchestrator != null)
        {
            _orchestrator.OnStateChanged -= HandleStateChanged;
            _orchestrator.OnQrCodeAvailable -= HandleQrCodeAvailable;
            _orchestrator.OnLoginCredentialsReceived -= HandleLoginCredentialsReceived;

            await _orchestrator.DisposeAsync();
            _orchestrator = null;
        }
    }
    public async ValueTask DisposeAsync()
    {
        await DisposeCurrentOrchestrator();
    }
}
