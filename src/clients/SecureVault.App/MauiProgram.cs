using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using SecureVault.App.Application;
using SecureVault.App.Infrastructure;
using System.Reflection;
using ZXing.Net.Maui.Controls;

namespace SecureVault.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();


        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });
        var a = Assembly.GetExecutingAssembly();
        using var stream = a.GetManifestResourceStream("SecureVault.App.appsettings.json");

        var configurationBuilder = new ConfigurationBuilder();
        if (stream != null)
        {
            configurationBuilder.AddJsonStream(stream);
        }

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddSingleton<App>();

#if DEBUG
        using var devStream = a.GetManifestResourceStream("SecureVault.App.appsettings.Development.json");

        if (devStream != null)
        {
            configurationBuilder.AddJsonStream(devStream);
        }
        builder.Services.AddBlazorWebViewDeveloperTools();
#if ANDROID
        Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("EnableDebugging", (handler, view) =>
        {
            if (handler.PlatformView is Android.Webkit.WebView)
            {
                Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
            }
        });
#endif
        builder.Logging.AddDebug();
#endif

        configurationBuilder.AddEnvironmentVariables();
        builder.Configuration.AddConfiguration(configurationBuilder.Build());

        builder.Services.AddLocalization();
        builder.Services.AddMudServices();
        builder.Services.AddAppServices();
        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddAuthorizationCore();
        builder.Services.AddLocalization();

        return builder.Build();
    }
}
