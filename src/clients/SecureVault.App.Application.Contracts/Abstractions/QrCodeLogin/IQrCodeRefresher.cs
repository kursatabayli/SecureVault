namespace SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;

public interface IQrCodeRefresher : IAsyncDisposable
{
  event Func<string, Task> OnQrCodeAvailable;
  event Func<string, Task> OnError;
  Task StartAsync(Func<Task<string>> switchChannelCallback, TimeSpan interval, CancellationToken cancellationToken);
  Task StopAsync();
}