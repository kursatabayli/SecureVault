using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Infrastructure.Context;
using SecureVault.Vault.Infrastructure.Repositories;

namespace SecureVault.Vault.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<IVaultItemsRepository, VaultItemsRepository>();

        services.AddScoped<MongoDbContext>();
        return services;
    }
}
