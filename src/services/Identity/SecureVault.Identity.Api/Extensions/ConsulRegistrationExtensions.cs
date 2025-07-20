using Consul;

namespace SecureVault.Identity.Api.Extensions
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
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("ConsulRegistration");
            logger.LogInformation("--- KONTROL -> ServiceName: {sn}, ServiceAddress: {sa}, ServicePort: {sp}, ConsulHost: {ch}",
            config["Consul:ServiceName"],
            config["Consul:ServiceAddress"],
            config["Consul:ServicePort"],
            config["Consul:Host"]);

            lifetime.ApplicationStarted.Register(() =>
            {
                try
                {
                    var serviceName = config["Consul:ServiceName"];
                    var serviceAddress = config["Consul:ServiceAddress"];
                    var servicePortRaw = config["Consul:ServicePort"];

                    if (string.IsNullOrEmpty(serviceName) || string.IsNullOrEmpty(serviceAddress) || string.IsNullOrEmpty(servicePortRaw))
                    {
                        logger.LogError("--> Consul yapılandırma bilgileri (ServiceName, ServiceAddress, ServicePort) eksik.");
                        return;
                    }

                    if (!int.TryParse(servicePortRaw, out var servicePort))
                    {
                        logger.LogError("--> Consul:ServicePort yapılandırması geçersiz.");
                        return;
                    }

                    var registration = new AgentServiceRegistration()
                    {
                        ID = serviceName,
                        Name = serviceName,
                        Address = serviceAddress,
                        Port = servicePort,
                        Tags = [serviceName, "api", "identity"],
                        Check = new AgentServiceCheck()
                        {
                            HTTP = "http://securevault-identity-api:8080/health",
                            Notes = "Checks /health endpoint",
                            Timeout = TimeSpan.FromSeconds(3),
                            Interval = TimeSpan.FromSeconds(10)
                        }
                    };

                    logger.LogInformation("--> {ServiceName} servisi Consul'a kaydediliyor: {ServiceAddress}:{Port}", serviceName, serviceAddress, servicePort);

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
                    var serviceAddress = config["Consul:ServiceAddress"];
                    if (!string.IsNullOrEmpty(serviceAddress))
                    {
                        logger.LogInformation("--> {serviceAddress} servisinin kaydı Consul'dan kaldırılıyor.", serviceAddress);
                        consulClient.Agent.ServiceDeregister(serviceAddress).Wait();
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
