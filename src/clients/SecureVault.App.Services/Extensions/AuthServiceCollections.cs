using Microsoft.AspNetCore.Components.Authorization;
using SecureVault.App.Services.AuthHelpers;
using SecureVault.App.Services.Constants;
using System.Net.Http.Headers;

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
            services.AddTransient<DeviceHeaderService>();

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

            services.AddHttpClient(nameof(ClientTypes.AuthenticatedClient), client =>
            {
                client.BaseAddress = new Uri(Endpoints.BaseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }).AddHttpMessageHandler<AuthTokenHandler>()
                  .ConfigurePrimaryHttpMessageHandler(configureHandler);

            services.AddHttpClient(nameof(ClientTypes.PublicClient), client =>
            {
                client.BaseAddress = new Uri(Endpoints.BaseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }).ConfigurePrimaryHttpMessageHandler(configureHandler);

            return services;
        }
    }
}
