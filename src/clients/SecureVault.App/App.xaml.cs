using SecureVault.App.Services.Interfaces;
using MauiApplication = Microsoft.Maui.Controls.Application;

namespace SecureVault.App;

public partial class App : MauiApplication
{
    private readonly IServiceProvider _serviceProvider;
    private IAppLifecycleManager? _lifecycleManager;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    protected override void OnStart()
    {
        base.OnStart();

    }

    protected override async void OnSleep()
    {
        base.OnSleep();
        if (_lifecycleManager != null)
        {
            await _lifecycleManager.OnSleep();
        }
    }

    protected override async void OnResume()
    {
        base.OnResume();
        if (_lifecycleManager != null)
        {
            await _lifecycleManager.OnResume();
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window
        {
            Page = new MainPage(),
            Title = "Secure Vault"
        };

        window.Created += async (s, e) =>
                    {
                        try
                        {
                            _lifecycleManager = _serviceProvider.GetRequiredService<IAppLifecycleManager>();

                            _lifecycleManager.Initialize();
                            await _lifecycleManager.OnStart();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"FATAL ERROR during AppLifecycleManager setup: {ex}");
                        }
                    };

        window.Destroying += async (s, e) =>
        {
            if (_lifecycleManager != null)
            {
                await _lifecycleManager.OnDestroying();
            }
        };

        return window;
    }
}
