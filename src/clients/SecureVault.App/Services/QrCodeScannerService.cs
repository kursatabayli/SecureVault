using SecureVault.App.Components.Helpers;
using MauiApplication = Microsoft.Maui.Controls.Application;

namespace SecureVault.App.Services
{
    public class QrCodeScannerService : IQrCodeScannerService
    {
        public async Task<string> ScanAsync()
        {
            var tcs = new TaskCompletionSource<string>();
            var mainPage = MauiApplication.Current?.Windows.FirstOrDefault()?.Page;
            if (mainPage != null)
            {
                await mainPage.Navigation.PushModalAsync(new QrScannerPage(tcs));
            }
            else
            {
                tcs.SetException(new InvalidOperationException("Ana uygulama penceresi (MainPage) bulunamadı."));
                // tcs.SetResult(null); 
            }

            return await tcs.Task;
        }
    }
}
