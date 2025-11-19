using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using SecureVault.ApiGateway.Helpers;
using SecureVault.ApiGateway.MiddleWares;
using SecureVault.ApiGateway.Services;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Serilog.Events;
using System.Net.Http.Headers;
using System.Text;
using Yarp.ReverseProxy.Transforms;

namespace SecureVault.ApiGateway;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        const string appName = "SecureVault.ApiGateway";

        builder.AddServiceDefaults();

        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Services(services)
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", appName)
            .Enrich.WithOpenTelemetrySpanId()
            .Enrich.WithOpenTelemetryTraceId()
            .WriteTo.Console()
            .WriteTo.Seq(
                builder.Configuration.GetConnectionString("seq"),
                restrictedToMinimumLevel: LogEventLevel.Information));

        Log.Information("{ApplicationName} service is starting.", appName);

        builder.Services.AddServiceDiscovery();

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownProxies.Clear();
            options.KnownNetworks.Clear();
        });

        builder.Services.AddScoped<IDpopJtiCache, RedisDpopJtiCache>();
        builder.Services.AddScoped<DpopJwtEventsHandler>();

        builder.AddSeqEndpoint("seq");

        builder.AddRedisDistributedCache("redis-cache");

        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(nameof(JwtSettings)));
        var jwtSettings = builder.Configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "DPoP";
            options.DefaultChallengeScheme = "DPoP";
        }).AddJwtBearer("DPoP", options =>
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
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var handler = context.HttpContext.RequestServices.GetRequiredService<DpopJwtEventsHandler>();
                    return handler.HandleMessageReceived(context);
                },
                OnTokenValidated = context =>
                {
                    var handler = context.HttpContext.RequestServices.GetRequiredService<DpopJwtEventsHandler>();
                    return handler.HandleTokenValidated(context);
                }
            };
        });

        builder.Services.AddAuthorization();

        builder.Configuration.AddJsonFile("yarp.json", optional: false, reloadOnChange: true);

        builder.Services.AddReverseProxy()
                        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
                        .AddServiceDiscoveryDestinationResolver()
                        .AddTransforms(builderContext =>
                        {
                            var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
                            var logger = loggerFactory.CreateLogger("SecureVault.ApiGateway.YarpTransforms");

                            builderContext.AddRequestTransform(transformContext =>
                            {
                                var dpopThumbprint = transformContext.HttpContext.Items["ValidatedDPoPThumbprint"] as string;

                                transformContext.ProxyRequest.Headers.Remove("DPoP");

                                if (dpopThumbprint is not null)
                                {
                                    logger.LogDebug("Transform: Adding 'X-DPoP-JKT' header for upstream service. JKT: {Jkt}", dpopThumbprint);
                                    transformContext.ProxyRequest.Headers.Add("X-DPoP-JKT", dpopThumbprint);
                                }
                                else
                                {
                                    logger.LogDebug("Transform: 'ValidatedDPoPThumbprint' not found in context (likely a Bearer token or non-DPoP request).");
                                }

                                if (transformContext.ProxyRequest.Headers.Authorization?.Scheme == "DPoP")
                                {
                                    var dpopAuthHeader = AuthenticationHeaderValue.Parse(transformContext.ProxyRequest.Headers.Authorization.ToString());
                                    transformContext.ProxyRequest.Headers.Authorization =
                                        new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, dpopAuthHeader.Parameter);

                                    logger.LogDebug("Transform: Converting 'Authorization: DPoP' to 'Authorization: Bearer' for upstream service.");
                                }
                                return ValueTask.CompletedTask;
                            });
                        });


        var app = builder.Build();

        app.UseSerilogRequestLogging();
        app.UseForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<TokenBlacklistMiddleware>();
        app.UseWebSockets();
        app.MapDefaultEndpoints();
        app.MapReverseProxy();

        try
        {
            Log.Information("Starting the application host for {ApplicationName}...", appName);
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "{ApplicationName} application host terminated unexpectedly.", appName);
        }
        finally
        {
            Log.Information("{ApplicationName} application shutting down.", appName);
            Log.CloseAndFlush();
        }
    }
}
