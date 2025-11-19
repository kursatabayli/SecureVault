using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SecureVault.Interaction.Api.Extensions;
using SecureVault.Interaction.Api.Features.Interaction;
using SecureVault.Interaction.Api.Features.QrLogin;
using SecureVault.Interaction.Api.Helpers;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Serilog.Events;
using StackExchange.Redis;
using System.Text;

namespace SecureVault.Interaction.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        const string appName = "SecureVault.Interaction.Api";


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

        builder.Services.AddControllers();
        builder.Services.RegisterServices();
        builder.Services.AddOpenApi();
        builder.Services.AddLocalization();

        builder.AddSeqEndpoint("seq");

        builder.AddRedisClient("redis-cache");
        builder.Services.AddAuthorization();

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

        var redisConnectionString = builder.Configuration.GetConnectionString("redis-cache");
        if (string.IsNullOrEmpty(redisConnectionString))
            throw new InvalidOperationException("Redis connection string 'redis-cache' was not found.");
        builder.Services.AddSignalR().AddStackExchangeRedis(redisConnectionString, options =>
        {
            options.Configuration.ChannelPrefix = RedisChannel.Literal("Interaction-Api");
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // app.UseHttpsRedirection();
        app.UseSerilogRequestLogging();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWebSockets();
        app.MapDefaultEndpoints();
        app.MapHub<InteractionHub>("/hubs/interaction-hub");
        app.MapHub<QrLoginHub>("/hubs/qr-login-hub");

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
