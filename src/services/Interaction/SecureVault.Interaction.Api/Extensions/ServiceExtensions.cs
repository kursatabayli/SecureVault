using SecureVault.Interaction.Api.Features.QrLogin.Contracts;
using SecureVault.Interaction.Api.Features.QrLogin.Services;

namespace SecureVault.Interaction.Api.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddScoped<IQrLoginChannelService, InMemoryQrLoginChannelService>();

            return services;
        }
    }
}
