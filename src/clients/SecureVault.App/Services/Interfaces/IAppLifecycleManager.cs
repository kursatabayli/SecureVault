namespace SecureVault.App.Services.Interfaces;

public interface IAppLifecycleManager : IAsyncDisposable
{
    void Initialize();
    Task OnStart();
    Task OnSleep();
    Task OnResume();
    Task OnDestroying();
}