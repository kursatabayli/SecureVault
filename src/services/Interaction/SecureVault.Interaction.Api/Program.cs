using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SecureVault.Interaction.Api.Extensions;
using SecureVault.Interaction.Api.Features.QrLogin.Hubs;
using SecureVault.Interaction.Api.Features.Sync.Hubs;
using SecureVault.Interaction.Api.Helpers;
using Serilog;
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
                            .ReadFrom.Configuration(context.Configuration)
                            .ReadFrom.Services(services)
                            .Enrich.FromLogContext()
                            .Enrich.WithProperty("ApplicationName", "SecureVault.Interaction.Api")
                            .Enrich.WithActivityId()
                            .Enrich.WithActivityTags());

            Log.Information("Uygulama başlatılıyor.");

            builder.Services.AddControllers();
            builder.Services.RegisterServices();
            builder.Services.AddOpenApi();
            builder.Services.AddLocalization();

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

            builder.Services.AddSignalR();
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
            app.MapHub<SyncHub>("/hubs/synchub");
            app.MapHub<QrLoginHub>("/hubs/qrlogin");

            app.Run();
        }
    }
}
