using SecureVault.App.Services;
using SecureVault.App.Services.Service;
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
            services.AddSingleton<IOtpService, OtpService>();
            services.AddScoped<IQrCodeScannerService, QrCodeScannerService>();

            return services;
        }
    }
}
