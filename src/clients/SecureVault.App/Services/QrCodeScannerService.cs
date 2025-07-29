using SecureVault.App.Components.Helpers;

namespace SecureVault.App.Services
{
    public class QrCodeScannerService : IQrCodeScannerService
    {
        public async Task<string> ScanAsync()
        {
            var tcs = new TaskCompletionSource<string>();
            await Application.Current.MainPage.Navigation.PushModalAsync(new QrScannerPage(tcs));
            return await tcs.Task;
        }
    }
}
