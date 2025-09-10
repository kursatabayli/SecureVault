namespace SecureVault.App.Services
{
    public interface IAppLifecycleManager : IAsyncDisposable
    {
        void Initialize();
        Task OnStart();
        Task OnSleep();
        Task OnResume();
        Task OnDestroying();
    }
}
