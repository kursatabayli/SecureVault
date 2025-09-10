using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using SecureVault.Identity.Api.Extensions;
using SecureVault.Identity.Api.MiddleWares;
using SecureVault.Identity.Application;
using SecureVault.Identity.Infrastructure.Context;
using SecureVault.Identity.Infrastructure.Helpers;
using SecureVault.Shared.RabbitMQ.Options;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using System.Net;
using System.Text;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace SecureVault.Identity.Api
{
    public class Program
    {
        public static void Main(string[] args)
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

                builder.Services.AddControllers();
                builder.Services.AddApplicationServices();
                builder.Services.AddDbContextConfiguration(builder.Configuration);
                builder.Services.RegisterServices();
                builder.Services.AddOpenApi();
                builder.Services.AddLocalization();

                builder.Services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();


                    var knownNetworks = builder.Configuration["ForwardedHeadersOptions:KnownNetworks"];
                    if (!string.IsNullOrEmpty(knownNetworks))
                    {
                        var cidrParts = knownNetworks.Split('/');
                        if (cidrParts.Length == 2 && IPAddress.TryParse(cidrParts[0], out var ipAddress) && int.TryParse(cidrParts[1], out var prefixLength))
                            options.KnownNetworks.Add(new IPNetwork(ipAddress, prefixLength));
                    }
                    //options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("::ffff:172.22.0.0"), 112));
                });

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

                builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
                var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
                builder.Services.AddConsul(builder.Configuration);
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


                builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
                var rabbitMqSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqOptions>();
                builder.Services.AddSingleton<IConnectionFactory>(sp => new ConnectionFactory
                {
                    Uri = new Uri(rabbitMqSettings.Uri),
                });

                builder.Services.AddAuthorization();

                var app = builder.Build();


                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();

                    using var scope = app.Services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    dbContext.Database.Migrate();
                }

                var supportedCultures = new[] { "en-US", "tr-TR" };
                app.UseRequestLocalization(new RequestLocalizationOptions()
                    .SetDefaultCulture(supportedCultures[0])
                    .AddSupportedCultures(supportedCultures)
                    .AddSupportedUICultures(supportedCultures));

                //app.UseHttpsRedirection();
                app.UseSerilogRequestLogging();
                app.UseMiddleware<CorrelationIdMiddleware>();
                app.UseForwardedHeaders();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapGet("/health", () => Results.Ok());
                app.RegisterWithConsul(app.Lifetime);
                app.MapControllers();

                app.Run();
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
