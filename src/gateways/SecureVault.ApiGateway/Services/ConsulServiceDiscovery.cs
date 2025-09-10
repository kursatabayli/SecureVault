using Consul;

namespace SecureVault.ApiGateway.Services
{
    public class ConsulServiceDiscovery : IServiceDiscovery
    {
        private readonly IConsulClient _consulClient;
        private readonly ILogger<ConsulServiceDiscovery> _logger;

        public ConsulServiceDiscovery(IConsulClient consulClient, ILogger<ConsulServiceDiscovery> logger)
        {
            _consulClient = consulClient;
            _logger = logger;
        }

        public async Task<Uri> GetServiceAddressAsync(string serviceName)
        {
            var queryResult = await _consulClient.Health.Service(serviceName, tag: null, passingOnly: true);

            var healthyServices = queryResult.Response;
            if (healthyServices == null || !healthyServices.Any())
            {
                _logger.LogError("{ServiceName} adresi için sağlıklı bir servis Consul'da bulunamadı.", serviceName);
                return null;
            }

            var service = healthyServices.ElementAt(new Random().Next(healthyServices.Length));

            var serviceAddress = $"{service.Service.Address}:{service.Service.Port}";
            _logger.LogInformation("{ServiceName} adresi, Consul'dan '{ServiceAddress}' olarak çözüldü.", serviceName, serviceAddress);

            return new Uri($"http://{serviceAddress}");
        }
    }
}
