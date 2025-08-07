using Microsoft.AspNetCore.Components.Authorization;
using Refit;
using SecureVault.App.Services.APIs;
using SecureVault.App.Services.AuthHelpers;
using SecureVault.App.Services.Constants;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SecureVault.App.Services.Extensions
{
    public static class AuthServiceCollections
    {
        public static IServiceCollection AuthServices(this IServiceCollection services)
        {
            services.AddScoped<CustomAuthStateProvider>();
            services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
            services.AddTransient<DeviceHeadersHandler>();
            services.AddTransient<AuthTokenHandler>();

            Func<HttpClientHandler> configureHandler = () => new HttpClientHandler
            {
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,

                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    if (DeviceInfo.Platform == DevicePlatform.Android && message.RequestUri.Host == "192.168.31.244")
                    {
                        Console.WriteLine("[SSL-DEBUG] Certificate validation bypassed for local Android dev.");
                        return true;
                    }
                    return errors == System.Net.Security.SslPolicyErrors.None;
                }
            };

            var refitSettings = new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
            };

            services.AddRefitClient<ISecureVaultApi>(refitSettings)
                .ConfigureHttpClient(client =>
                {
                    client.BaseAddress = new Uri("https://localhost:7202/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                })
                .AddHttpMessageHandler<DeviceHeadersHandler>()
                .AddHttpMessageHandler<AuthTokenHandler>()
                .ConfigurePrimaryHttpMessageHandler(configureHandler);

            return services;
        }
    }
}
