using ZXing.Net.Maui;

namespace SecureVault.App.Components.Pages.VaultItem.Helpers;

public partial class QrScannerPage : ContentPage
{
    private readonly TaskCompletionSource<string> _taskCompletionSource;

    public QrScannerPage(TaskCompletionSource<string> taskCompletionSource)
    {
        InitializeComponent();
        _taskCompletionSource = taskCompletionSource;
    }

    private void BarcodesDetected_Handler(object sender, BarcodeDetectionEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            barcodeReader.IsDetecting = false;

            string result = e.Results.FirstOrDefault()?.Value;
            _taskCompletionSource.TrySetResult(result);

            await Navigation.PopModalAsync();
        });
    }
    protected override bool OnBackButtonPressed()
    {
        _taskCompletionSource.TrySetResult(null);

        return base.OnBackButtonPressed();
    }
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        _taskCompletionSource.TrySetResult(null);
    }
}