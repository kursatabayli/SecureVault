using SecureVault.App.Components.Pages.VaultItem.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;

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
