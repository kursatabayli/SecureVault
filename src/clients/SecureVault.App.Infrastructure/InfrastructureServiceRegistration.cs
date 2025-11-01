using Microsoft.Extensions.Configuration;
using Refit;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Database;
using SecureVault.App.Application.Contracts.Abstractions.Device;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;
using SecureVault.App.Infrastructure.Helpers;
using SecureVault.App.Infrastructure.HttpHandlers;
using SecureVault.App.Infrastructure.Repositories;
using SecureVault.App.Infrastructure.Services.Api;
using SecureVault.App.Infrastructure.Services.Database;
using SecureVault.App.Infrastructure.Services.Device;
using SecureVault.App.Infrastructure.Services.QrCodeLogin;
using SecureVault.App.Infrastructure.Services.QrCodeLogin.MessageHandlers;
using SecureVault.App.Infrastructure.Services.Storage;
using SecureVault.App.Infrastructure.Services.Sync;
using SecureVault.App.Infrastructure.Services.Sync.Handlers;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json;

namespace SecureVault.App.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
        {
            //api
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IVaultItemService, VaultItemService>();
            services.AddScoped<IUserSessionService, UserSessionService>();
            services.AddScoped<IRecoveryDataService, RecoveryDataService>();
            services.AddScoped<IRegisterService, RegisterService>();
            services.AddScoped<IInteractionService, InteractionService>();
            //device
            services.AddScoped<IDeviceInfoService, DeviceInfoService>();
            //persistence

            services.AddSingleton(Preferences.Default);
            services.AddSingleton(SecureStorage.Default);
            services.AddScoped<IStorageService, StorageService>();
            //http handlers
            services.AddTransient<DeviceHeadersHandler>();
            services.AddTransient<AuthTokenHandler>();
            services.AddTransient<AddBearerTokenHandler>();
            services.AddTransient<PollyResiliencyHandler>();
            services.AddSingleton<IHttpHandlerPipelineBuilder, HttpHandlerPipelineBuilder>();

            //repositories
            services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IPasswordRepository, PasswordRepository>();
            services.AddScoped<ITwoFactorAuthCodeRepository, TwoFactorAuthCodeRepository>();


            //db context
            services.AddScoped<IRealmService, RealmService>();
            services.AddSingleton<IDatabaseManager, DatabaseManager>();

            //sync services
            services.AddSingleton(Connectivity.Current);
            services.AddSingleton<IBackgroundSyncService, BackgroundSyncService>();
            services.AddScoped<ISyncDataProcessor, SyncDataProcessor>();
            services.AddScoped<ISyncProcessor, SyncProcessor<PasswordEntity>>();
            services.AddScoped<ISyncProcessor, SyncProcessor<TwoFactorAuthCodeEntity>>();
            services.AddScoped<PasswordSyncHandler>();
            services.AddScoped<TwoFactorAuthSyncHandler>();
            services.AddScoped<IEntityDataProcessor>(sp => sp.GetRequiredService<PasswordSyncHandler>());
            services.AddScoped<IEntityDataProcessor>(sp => sp.GetRequiredService<TwoFactorAuthSyncHandler>());
            services.AddScoped<IEntityPayloadFactory<PasswordEntity>>(sp => sp.GetRequiredService<PasswordSyncHandler>());
            services.AddScoped<IEntityPayloadFactory<TwoFactorAuthCodeEntity>>(sp => sp.GetRequiredService<TwoFactorAuthSyncHandler>());
            services.AddSingleton<ISyncConnectionService, SyncConnectionService>();
            services.AddSingleton<ISyncLock, SyncLock>();
            services.AddSingleton<IActiveSessionTracker, ActiveSessionTracker>();

            //qr code login
            services.AddScoped<IQrLoginOrchestrator, QrLoginOrchestratorService>();
            services.AddScoped<IQrLoginContext>(sp => sp.GetRequiredService<IQrLoginOrchestrator>() as QrLoginOrchestratorService);
            services.AddScoped<IQrHubConnection, QrHubConnection>();
            services.AddScoped<ISecureChannelManager, SecureChannelManager>();

            services.AddScoped<IMessageHandler, AuthorizationApprovedHandler>();
            services.AddScoped<IMessageHandler, AuthorizationDeniedHandler>();
            services.AddScoped<IMessageHandler, ChannelReadyHandler>();
            services.AddScoped<IMessageHandler, DeviceInfoRequestHandler>();
            services.AddScoped<IMessageHandler, EncryptedLoginDataHandler>();
            services.AddScoped<IMessageHandler, PublicKeyHandler>();

            services.Configure<ApiSettings>(config.GetSection(nameof(ApiSettings)));
            services.Configure<SignalRSettings>(config.GetSection(nameof(SignalRSettings)));
            services.Configure<QrCodeSettings>(config.GetSection(nameof(QrCodeSettings)));

            var apiSettings = config.GetSection(nameof(ApiSettings)).Get<ApiSettings>()
                    ?? throw new InvalidOperationException("ApiSettings not found.");

            //refit
            static HttpClientHandler configureHandler(string machineName) => new()
            {
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
#if DEBUG
                    if (message.RequestUri.Host.Equals(machineName, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[SSL-DEBUG] Certificate validation bypassed for local dev host: {machineName}");
                        return true;
                    }
#endif
                    return errors == SslPolicyErrors.None;
                }
            };
            RefitSettings refitSettings = new()
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
            };
            void configureClient(HttpClient client)
            {
                client.BaseAddress = new Uri(apiSettings.BaseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            services.AddRefitClient<ISecureVaultAuthorizeApi>(refitSettings)
                    .ConfigureHttpClient(configureClient)
                    .AddHttpMessageHandler<PollyResiliencyHandler>()
                    .AddHttpMessageHandler<DeviceHeadersHandler>()
                    .AddHttpMessageHandler<AuthTokenHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => configureHandler(apiSettings.DevMachineName));

            services.AddRefitClient<ISecureVaultAnonymousApi>(refitSettings)
                    .ConfigureHttpClient(configureClient)
                    .AddHttpMessageHandler<PollyResiliencyHandler>()
                    .AddHttpMessageHandler<DeviceHeadersHandler>()
                    .AddHttpMessageHandler<AddBearerTokenHandler>()
                    .ConfigurePrimaryHttpMessageHandler(() => configureHandler(apiSettings.DevMachineName));

            return services;
        }
    }
}
