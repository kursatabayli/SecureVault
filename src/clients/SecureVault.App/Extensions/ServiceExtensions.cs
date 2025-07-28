using Nager.PublicSuffix;
using Nager.PublicSuffix.RuleProviders;
using SecureVault.App.Services;
using SecureVault.App.Services.Service.Contracts;
using SecureVault.App.Services.Service.Implementations;


namespace SecureVault.App.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>(); 
            services.AddScoped<IHashService, HashService>();
            services.AddScoped<IDeviceInfoService, DeviceInfoService>();
            //services.AddScoped<INetworkService, NetworkService>();
            services.AddScoped(typeof(IVaultItemService<>), typeof(VaultItemService<>));
            services.AddSingleton<ICryptoService, AesGcmCryptoService>();
            services.AddScoped<IBouncyCastleCryptoService, BouncyCastleCryptoService>();
            services.AddScoped<IApiClient, ApiClient>();
            services.AddScoped<IUserSessionService, UserSessionService>();
            services.AddSingleton<IOtpService, OtpService>();
            services.AddScoped<IQrCodeScannerService, QrCodeScannerService>();

            services.AddSingleton<IDomainParser>(serviceProvider =>
            {
                var ruleProvider = new LocalFileRuleProvider("public_suffix_list.dat");
                ruleProvider.BuildAsync().GetAwaiter().GetResult();
                return new DomainParser(ruleProvider);
            });

            services.AddSingleton<LogoutService>();

            return services;
        }
    }
}
