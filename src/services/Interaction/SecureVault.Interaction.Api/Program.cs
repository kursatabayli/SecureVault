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

namespace SecureVault.Interaction.Api
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
                            .Enrich.WithProperty("ApplicationName", "SecureVault.Interaction.Api")
                            .Enrich.WithOpenTelemetrySpanId()
                            .Enrich.WithOpenTelemetryTraceId()
                            .WriteTo.Console()
                            .WriteTo.Seq(
                                builder.Configuration.GetConnectionString("seq"),
                                restrictedToMinimumLevel: LogEventLevel.Information));

            Log.Information("Uygulama başlatılıyor.");

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
                throw new InvalidOperationException("Redis bağlantı dizesi 'redis-cache' bulunamadı.");
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

            app.Run();
        }
    }
}
