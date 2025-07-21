using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using SecureVault.App.Services.Extensions;
using SecureVault.App.Extensions;

namespace SecureVault.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();


            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            builder.Services.AddLocalization();
            builder.Services.AddMudServices();
            builder.Services.RegisterServices();
            builder.Services.AuthServices();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddLocalization();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
#if ANDROID
            BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("EnableDebugging", (handler, view) =>
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
