using Microsoft.Extensions.Options;
using SecureVault.App.Infrastructure.Helpers;

namespace SecureVault.App.Infrastructure.HttpHandlers
{
    internal class HttpHandlerPipelineBuilder : IHttpHandlerPipelineBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ApiSettings _apiSettings;

        public HttpHandlerPipelineBuilder(IServiceProvider serviceProvider, IOptions<ApiSettings> apiSettings)
        {
            _serviceProvider = serviceProvider;
            _apiSettings = apiSettings.Value;
        }

        public DelegatingHandler CreatePipeline()
        {
            var customFinalHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
#if DEBUG
                    var machineName = _apiSettings.DevMachineName;
                    if (!string.IsNullOrEmpty(machineName) && message.RequestUri.Host.Equals(machineName, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[SSL-DEBUG] Certificate validation bypassed for {machineName} via PipelineBuilder.");
                        return true;
                    }
#endif
                    return errors == System.Net.Security.SslPolicyErrors.None;
                }
            };

            var pollyResiliencyHandler = _serviceProvider.GetRequiredService<PollyResiliencyHandler>();
            var deviceHeadersHandler = _serviceProvider.GetRequiredService<DeviceHeadersHandler>();
            var authTokenHandler = _serviceProvider.GetRequiredService<AuthTokenHandler>();

            pollyResiliencyHandler.InnerHandler = deviceHeadersHandler;
            deviceHeadersHandler.InnerHandler = authTokenHandler;
            authTokenHandler.InnerHandler = customFinalHandler;

            return pollyResiliencyHandler;
        }
    }
}
