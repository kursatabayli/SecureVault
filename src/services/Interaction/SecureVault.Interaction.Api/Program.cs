using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SecureVault.Interaction.Api.Extensions;
using SecureVault.Interaction.Api.Features.QrLogin.Hubs;
using SecureVault.Interaction.Api.Features.Sync.Hubs;
using SecureVault.Interaction.Api.Helpers;
using SecureVault.Interaction.Api.MiddleWares;
using Serilog;
using Serilog.Events;
using System.Text;

namespace SecureVault.Interaction.Api
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
                Log.Information("Uygulama ba�lat�l�yor.");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console());

                builder.Services.AddControllers();
                builder.Services.RegisterServices();
                builder.Services.AddOpenApi();
                builder.Services.AddConsul(builder.Configuration);
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

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAppOrigin",
                        policy =>
                        {
                            policy.WithOrigins("http://localhost:7202")
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

                app.UseSerilogRequestLogging();
                app.UseMiddleware<CorrelationIdMiddleware>();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapGet("/health", () => Results.Ok());
                app.RegisterWithConsul(app.Lifetime);
                app.MapControllers();
                app.UseCors("AllowAppOrigin");
                app.MapHub<SyncHub>("/hubs/synchub");
                app.MapHub<QrLoginHub>("/hubs/qrlogin");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Uygulama ba�lat�l�rken kritik bir hata olu�tu.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
