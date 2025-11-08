using Microsoft.Extensions.Options;
using SecureVault.App.Infrastructure.Helpers;

namespace SecureVault.App.Infrastructure.HttpHandlers
{
    internal class HttpHandlerPipelineBuilder : IHttpHandlerPipelineBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ApiSettings _apiSettings;
        private readonly string _devHost;

        public HttpHandlerPipelineBuilder(IServiceProvider serviceProvider, IOptions<ApiSettings> apiSettings)
        {
            _serviceProvider = serviceProvider;
            _apiSettings = apiSettings.Value;
            _devHost = string.Empty;
            if (!string.IsNullOrEmpty(_apiSettings.BaseUrl) && Uri.TryCreate(_apiSettings.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                _devHost = baseUri.Host;
            }
        }

        public DelegatingHandler CreatePipeline()
        {
            var customFinalHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
#if DEBUG
                    if (!string.IsNullOrEmpty(_devHost) && message.RequestUri.Host.Equals(_devHost, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[SSL-DEBUG] Certificate validation bypassed for {_devHost} via PipelineBuilder.");
                        return true;
                    }
#endif
                    return errors == System.Net.Security.SslPolicyErrors.None;
                }
            };

            var pollyResiliencyHandler = _serviceProvider.GetRequiredService<PollyResiliencyHandler>();
            var deviceHeadersHandler = _serviceProvider.GetRequiredService<DeviceHeadersHandler>();
            var refreshTokenHandler = _serviceProvider.GetRequiredService<RefreshTokenHandler>();
            var dpopHandler = _serviceProvider.GetRequiredService<DpopHandler>();

            pollyResiliencyHandler.InnerHandler = deviceHeadersHandler;
            deviceHeadersHandler.InnerHandler = refreshTokenHandler;
            refreshTokenHandler.InnerHandler = dpopHandler;
            dpopHandler.InnerHandler = customFinalHandler;

            return pollyResiliencyHandler;
        }
    }
}
