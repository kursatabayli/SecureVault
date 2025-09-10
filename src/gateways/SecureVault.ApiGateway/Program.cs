using Consul;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using SecureVault.ApiGateway.Extensions;
using SecureVault.ApiGateway.Handlers;
using SecureVault.ApiGateway.Helpers;
using SecureVault.ApiGateway.Services;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using System.Text;

namespace SecureVault.ApiGateway
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Uygulama baþlatýlýyor.");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console());

                var consulAddress = new Uri(builder.Configuration.GetConnectionString("ConsulHost"));
                builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(config =>
                {
                    config.Address = consulAddress;
                }));

                builder.Services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();
                builder.Services.AddTransient<ServiceDiscoveryDelegatingHandler>();
                builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
                builder.Services.AddHttpClient("VaultApiClient", client =>
                {
                    client.BaseAddress = new Uri("http://VaultService/");
                })
                .AddHttpMessageHandler<ServiceDiscoveryDelegatingHandler>()
                .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
                builder.Services.AddHttpClient("IdentityApiClient", client =>
                {
                    client.BaseAddress = new Uri("http://IdentityService/");
                })
                .AddHttpMessageHandler<ServiceDiscoveryDelegatingHandler>()
                .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
                builder.Services.AddHttpClient("InteractionApiClient", client =>
                {
                    client.BaseAddress = new Uri("http://InteractionService/");
                })
                .AddHttpMessageHandler<ServiceDiscoveryDelegatingHandler>()
                .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();


                builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

                    if (string.IsNullOrEmpty(redisConnectionString))
                    {
                        throw new InvalidOperationException("Redis connection string 'Redis:ConnectionString' not found in configuration.");
                    }

                    var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);

                    configurationOptions.AbortOnConnectFail = false;

                    return ConnectionMultiplexer.Connect(configurationOptions);
                });

                builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(nameof(JwtSettings)));
                var jwtSettings = builder.Configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>();

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

                builder.Services.AddAuthorization();

                builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

                builder.Services.AddOcelot(builder.Configuration).AddConsul<MyConsulServiceBuilder>();

                var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>();


                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("SecureVaultApp",
                        policyBuilder =>
                        {
                            policyBuilder.WithOrigins(origins ?? [])
                                         .AllowAnyMethod()
                                         .AllowAnyHeader()
                                         .AllowCredentials()
                                         .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                        });
                });

                var app = builder.Build();

                app.UseSerilogRequestLogging();
                app.UseHttpsRedirection();
                app.UseCors("SecureVaultApp");
                app.UseRouting();
                app.UseAuthentication();
                app.AddMiddlewares();
                app.UseAuthorization();
                app.UseWebSockets();
                await app.UseOcelot();

                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Uygulama baþlatýlýrken kritik bir hata oluþtu.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
