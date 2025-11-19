using Microsoft.Extensions.Logging;
using SecureVault.App.Components.Helpers;
using SecureVault.App.Services.Interfaces;
using MauiApplication = Microsoft.Maui.Controls.Application;

namespace SecureVault.App.Services.Implementations;

public class QrCodeScannerService : IQrCodeScannerService
{
    private readonly ILogger<QrCodeScannerService> _logger;

    public QrCodeScannerService(ILogger<QrCodeScannerService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ScanAsync()
    {
        _logger.LogInformation("QR code scanning requested.");
        var tcs = new TaskCompletionSource<string>();

        var mainPage = MauiApplication.Current?.Windows.FirstOrDefault()?.Page;

        if (mainPage != null)
        {
            _logger.LogDebug("MainPage found. Pushing modal scanner page.");
            await mainPage.Navigation.PushModalAsync(new QrScannerPage(tcs));
        }
        else
        {
            _logger.LogError("Critical: MainPage (the main application window) was not found.");
            tcs.SetException(new InvalidOperationException("Main application window (MainPage) was not found."));
        }

        return await tcs.Task;
    }
}
