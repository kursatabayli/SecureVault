using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SecureVault.ApiGateway.Helpers;
using SecureVault.ApiGateway.MiddleWares;
using Serilog;
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
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationName", "SecureVault.ApiGateway")
                .Enrich.WithActivityId()
                .Enrich.WithActivityTags());

            Log.Information("Uygulama başlatılıyor.");

            builder.Services.AddServiceDiscovery();

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
            app.UseMiddleware<TokenBlacklistMiddleware>();
            app.UseAuthorization();
            app.UseWebSockets();
            app.MapReverseProxy();

            app.Run();
        }
    }
}
