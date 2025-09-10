using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using SecureVault.App.Application;
using SecureVault.App.Infrastructure;
using System.Reflection;
using ZXing.Net.Maui.Controls;

namespace SecureVault.App
{
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

            var config = new ConfigurationBuilder()
                        .AddJsonStream(stream)
                        .Build();

            builder.Configuration.AddConfiguration(config);
            builder.Services.AddMauiBlazorWebView();

            builder.Services.AddLocalization();
            builder.Services.AddMudServices();
            builder.Services.AddAppServices(builder.Configuration);
            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddApplicationServices();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddLocalization();

            builder.Services.AddSingleton<App>();

#if DEBUG
            using var devStream = a.GetManifestResourceStream("SecureVault.App.appsettings.Development.json");
            if (devStream != null)
            {
                var devConfig = new ConfigurationBuilder()
                    .AddJsonStream(devStream)
                    .Build();
                builder.Configuration.AddConfiguration(devConfig);
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

            return builder.Build();
        }
    }
}
