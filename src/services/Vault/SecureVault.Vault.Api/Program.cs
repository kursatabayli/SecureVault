using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SecureVault.Vault.Api.Extensions;
using SecureVault.Vault.Api.Helpers;
using SecureVault.Vault.Api.MiddleWares;
using SecureVault.Vault.Application;
using Serilog;
using Serilog.Events;
using System.Text;

namespace SecureVault.Vault.Api
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
                builder.Services.AddApplicationServices();
                builder.Services.AddMongoDbConfiguration(builder.Configuration);
                builder.Services.RegisterServices();
                builder.Services.AddOpenApi();
                builder.Services.AddLocalization();

                builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
                var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
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
                builder.Services.AddConsul(builder.Configuration);


                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                }

                //app.UseHttpsRedirection();
                app.UseSerilogRequestLogging();
                app.UseMiddleware<CorrelationIdMiddleware>();
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
                Log.Fatal(ex, "Uygulama ba�lat�l�rken kritik bir hata olu�tu.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
