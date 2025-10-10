using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Nager.PublicSuffix;
using Nager.PublicSuffix.Models;
using Nager.PublicSuffix.RuleParsers;
using Nager.PublicSuffix.RuleProviders;
using SecureVault.App.Application.Contracts.Abstractions.UI;
using SecureVault.App.Auth;
using SecureVault.App.Infrastructure.Helpers.Data;
using SecureVault.App.Services;

namespace SecureVault.App
{
    public static class AppServiceRegistration
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IAppLifecycleManager, AppLifecycleManager>();
            services.AddSingleton<CustomAuthStateProvider>();
            services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
            services.AddSingleton<IAuthenticationStateNotifier>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
            services.AddSingleton<IOtpService, OtpService>();
            services.AddScoped<IQrCodeScannerService, QrCodeScannerService>();
            services.AddScoped<ITwoFactorAuthCodeCreationService, TwoFactorAuthCodeCreationService>();
            services.AddScoped<IQrCodeGenerateService, QrCodeGenerateService>();
            services.AddScoped<DatabaseManager>();

            services.AddSingleton<IDomainParser>(serviceProvider =>
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("public_suffix_list.dat")
                                             .GetAwaiter()
                                             .GetResult();

                using var reader = new StreamReader(stream);
                var fileContent = reader.ReadToEnd();

                var ruleParser = new TldRuleParser(TldRuleDivisionFilter.All);
                var rules = ruleParser.ParseRules(fileContent);

                var ruleProvider = new StaticRuleProvider(rules);

                return new DomainParser(ruleProvider);
            });

            return services;
        }
    }
}
