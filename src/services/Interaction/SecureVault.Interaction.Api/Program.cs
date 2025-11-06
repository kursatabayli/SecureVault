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

            var allowedOrigin = builder.Configuration["CorsSettings:AllowedOrigin"];

            if (string.IsNullOrEmpty(allowedOrigin))
                throw new InvalidOperationException("CORS origin 'CorsSettings:AllowedOrigin' ayarlanmamış.");

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAppOrigin",
                    policy =>
                    {
                        policy.WithOrigins(allowedOrigin)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
            });

            builder.Services.AddSignalR().AddStackExchangeRedis(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("redis-cache");

                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException("Redis bağlantı dizesi 'redis-cache' bulunamadı.");

                options.Configuration = ConfigurationOptions.Parse(connectionString);
                options.Configuration.AbortOnConnectFail = false;
            });
            builder.Services.AddAuthorization();


            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // app.UseHttpsRedirection();
            app.UseSerilogRequestLogging();
            app.UseRouting();
            app.UseCors("AllowAppOrigin");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapDefaultEndpoints();
            app.MapControllers();
            app.MapHub<InteractionHub>("/hubs/interaction-hub");
            app.MapHub<QrLoginHub>("/hubs/qr-login-hub");

            app.Run();
        }
    }
}
