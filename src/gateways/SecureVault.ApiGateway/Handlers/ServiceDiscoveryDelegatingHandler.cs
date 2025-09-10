using Consul;
using SecureVault.ApiGateway.Services;
using System.Net;

namespace SecureVault.ApiGateway.Handlers
{
    public class ServiceDiscoveryDelegatingHandler : DelegatingHandler
    {
        private readonly IServiceDiscovery _serviceDiscovery;

        public ServiceDiscoveryDelegatingHandler(IServiceDiscovery serviceDiscovery)
        {
            _serviceDiscovery = serviceDiscovery;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var serviceName = request.RequestUri.Host;

            var serviceUri = await _serviceDiscovery.GetServiceAddressAsync(serviceName);

            if (serviceUri == null)
                throw new HttpRequestException($"Unable to find a healthy instance for the service '{serviceName}' in Consul.");

            var newUri = new UriBuilder(request.RequestUri)
            {
                Host = serviceUri.Host,
                Port = serviceUri.Port
            }.Uri;

            request.RequestUri = newUri;

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
