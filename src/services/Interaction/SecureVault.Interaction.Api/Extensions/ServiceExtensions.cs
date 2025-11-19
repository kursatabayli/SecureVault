using SecureVault.Interaction.Api.Features.Interaction.Contracts;
using SecureVault.Interaction.Api.Features.Interaction.Services;
using SecureVault.Interaction.Api.Features.QrLogin.Contracts;
using SecureVault.Interaction.Api.Features.QrLogin.Services;

namespace SecureVault.Interaction.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<IDevicePresenceService, RedisDevicePresenceService>();
        services.AddScoped<IQrLoginChannelService, RedisQrLoginChannelService>();

        return services;
    }
}
