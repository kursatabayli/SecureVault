using SecureVault.App.Services;
using MauiApplication = Microsoft.Maui.Controls.Application;

namespace SecureVault.App
{
    public partial class App : MauiApplication
    {
        private readonly IAppLifecycleManager _lifecycleManager;

        public App(IAppLifecycleManager lifecycleManager)
        {
            InitializeComponent();
            MainPage = new MainPage();

            _lifecycleManager = lifecycleManager;
            _lifecycleManager.Initialize();
        }

        protected override async void OnStart()
        {
            base.OnStart();
            await _lifecycleManager.OnStart();
        }

        protected override async void OnSleep()
        {
            base.OnSleep();
            await _lifecycleManager.OnSleep();
        }

        protected override async void OnResume()
        {
            base.OnResume();
            await _lifecycleManager.OnResume();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);

            window.Destroying += async (s, e) =>
            {
                await _lifecycleManager.OnDestroying();
            };
            window.Title = "Secure Vault";

            return window;
        }
    }
}
