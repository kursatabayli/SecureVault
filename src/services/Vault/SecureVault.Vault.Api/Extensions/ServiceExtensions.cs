using SecureVault.Vault.Application.Contracts.RepositoryContracts;
using SecureVault.Vault.Application.Contracts.ServicesContracts;
using SecureVault.Vault.Infrastructure.Repositories;
using SecureVault.Vault.Infrastructure.Services;

namespace SecureVault.Vault.Api.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
            services.AddScoped<IVaultItemsRepository, VaultItemsRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }
    }
}
