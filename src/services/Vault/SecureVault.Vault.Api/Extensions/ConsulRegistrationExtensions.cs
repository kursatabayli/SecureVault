using Consul;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace SecureVault.Vault.Api.Extensions
{
    public static class ConsulRegistrationExtensions
    {
        public static IServiceCollection AddConsul(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
            {
                var address = configuration["Consul:Host"];
                consulConfig.Address = new Uri(address!);
            }));

            return services;
        }

        public static IApplicationBuilder RegisterWithConsul(this IApplicationBuilder app, IHostApplicationLifetime lifetime)
        {
            var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();
            var config = app.ApplicationServices.GetRequiredService<IConfiguration>();
            var loggingFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggingFactory.CreateLogger("ConsulRegistration");

            lifetime.ApplicationStarted.Register(() =>
            {
                try
                {
                    var server = app.ApplicationServices.GetRequiredService<IServer>();
                    var addressFeature = server.Features.Get<IServerAddressesFeature>();

                    if (addressFeature == null || !addressFeature.Addresses.Any())
                    {
                        logger.LogError("--> Sunucu adresi alınamadı. Consul kaydı başarısız.");
                        return;
                    }

                    var address = addressFeature.Addresses.First();
                    var uri = new Uri(address);

                    var serviceName = config["Consul:ServiceName"];
                    var serviceAddress = config["Consul:ServiceAddress"];
                    var serviceId = $"{serviceName}-{uri.Port}";

                    var registration = new AgentServiceRegistration()
                    {
                        ID = serviceId,
                        Name = serviceName,
                        Address = serviceAddress,
                        Port = uri.Port,
                        Tags = [serviceName],
                        Check = new AgentServiceCheck()
                        {
                            HTTP = "http://securevault-vault-api:8080/health",
                            Notes = "Checks /health endpoint",
                            Timeout = TimeSpan.FromSeconds(3),
                            Interval = TimeSpan.FromSeconds(10)
                        }
                    };

                    logger.LogInformation($"--> {serviceName} servisi Consul'a kaydediliyor.");

                    consulClient.Agent.ServiceDeregister(registration.ID).Wait();
                    consulClient.Agent.ServiceRegister(registration).Wait();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "--> Servis Consul'a kaydedilirken bir hata oluştu.");
                }
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                try
                {
                    var server = app.ApplicationServices.GetRequiredService<IServer>();
                    var addressFeature = server.Features.Get<IServerAddressesFeature>();
                    if (addressFeature != null && addressFeature.Addresses.Any())
                    {
                        var address = addressFeature.Addresses.First();
                        var uri = new Uri(address);
                        var serviceName = config["Consul:ServiceName"];
                        var serviceId = $"{serviceName}-{uri.Port}";

                        logger.LogInformation($"--> {serviceName} servisi Consul'dan kaldırılıyor.");
                        consulClient.Agent.ServiceDeregister(serviceId).Wait();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "--> Servis Consul'dan kaldırılırken bir hata oluştu.");
                }
            });

            return app;
        }
    }
}
