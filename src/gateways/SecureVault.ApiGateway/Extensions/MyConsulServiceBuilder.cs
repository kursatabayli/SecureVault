using Consul;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Consul.Interfaces;

namespace SecureVault.ApiGateway.Extensions
{
    public class MyConsulServiceBuilder : DefaultConsulServiceBuilder
    {
        public MyConsulServiceBuilder(IHttpContextAccessor httpContextAccessor, IConsulClientFactory clientFactory, IOcelotLoggerFactory loggerFactory)
            : base(httpContextAccessor, clientFactory, loggerFactory) { }

        protected override string GetDownstreamHost(ServiceEntry entry, Node node)
        {
            return entry.Service.Address;
        }
    }
}
