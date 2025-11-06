using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using SecureVault.ApiGateway.Helpers;
using SecureVault.ApiGateway.MiddleWares;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Serilog.Events;
using System.Text;

namespace SecureVault.ApiGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.AddServiceDefaults();

            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Services(services)
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationName", "SecureVault.ApiGateway")
                .Enrich.WithOpenTelemetrySpanId()
                .Enrich.WithOpenTelemetryTraceId()
                .WriteTo.Console()
                .WriteTo.Seq(
                    builder.Configuration.GetConnectionString("seq"),
                    restrictedToMinimumLevel: LogEventLevel.Information));

            Log.Information("Uygulama başlatılıyor.");

            builder.Services.AddServiceDiscovery();

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownProxies.Clear();
                options.KnownNetworks.Clear();
            });

            builder.AddSeqEndpoint("seq");

            builder.AddRedisClient("redis-cache");

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

            builder.Configuration.AddJsonFile("yarp.json", optional: false, reloadOnChange: true);

            builder.Services.AddReverseProxy()
                            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
                            .AddServiceDiscoveryDestinationResolver();

            var app = builder.Build();

            app.UseSerilogRequestLogging();
            app.UseForwardedHeaders();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseMiddleware<TokenBlacklistMiddleware>();
            app.UseAuthorization();
            app.UseWebSockets();
            app.MapDefaultEndpoints();
            app.MapReverseProxy();

            app.Run();
        }
    }
}
