using Microsoft.Extensions.DependencyInjection;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Services;

namespace SecureVault.App.Application
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IHashService, HashService>();
            services.AddSingleton<ICryptoService, AesGcmCryptoService>();
            services.AddScoped<IBouncyCastleCryptoService, BouncyCastleCryptoService>();
            services.AddSingleton<IBip39RecoveryKeyService, Bip39RecoveryKeyService>();
            services.AddSingleton<ILocalVaultService, LocalVaultService>();

            services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly));

            return services;
        }
    }
}
