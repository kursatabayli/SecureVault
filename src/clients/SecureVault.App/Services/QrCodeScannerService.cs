using SecureVault.App.Components.Helpers;
using MauiApplication = Microsoft.Maui.Controls.Application;

namespace SecureVault.App.Services
{
    public class QrCodeScannerService : IQrCodeScannerService
    {
        public async Task<string> ScanAsync()
        {
            var tcs = new TaskCompletionSource<string>();
            await MauiApplication.Current.MainPage.Navigation.PushModalAsync(new QrScannerPage(tcs));
            return await tcs.Task;
        }
    }
}
