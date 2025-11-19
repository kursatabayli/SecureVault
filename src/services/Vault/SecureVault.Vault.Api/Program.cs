using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SecureVault.Vault.Api.Extensions;
using SecureVault.Vault.Api.Helpers;
using SecureVault.Vault.Application;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Serilog.Events;
using System.Text;

namespace SecureVault.Vault.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        const string appName = "SecureVault.Vault.Api";

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
        builder.Services.AddApplicationServices();
        builder.Services.RegisterServices();
        builder.Services.AddOpenApi();
        builder.Services.AddLocalization();

        builder.AddSeqEndpoint("seq");

        builder.Services.AddMongoDbConfiguration(builder);

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


        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // app.UseHttpsRedirection();
        app.UseSerilogRequestLogging();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapDefaultEndpoints();
        app.MapControllers();

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
