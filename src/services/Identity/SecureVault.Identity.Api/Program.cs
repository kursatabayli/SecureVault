using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using SecureVault.Identity.Api.Extensions;
using SecureVault.Identity.Application;
using SecureVault.Identity.Infrastructure.Helpers;
using StackExchange.Redis;
using System.Net;
using System.Text;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace SecureVault.Identity.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddApplicationServices();
            builder.Services.AddDbContextConfiguration(builder.Configuration);
            builder.Services.RegisterServices();
            builder.Services.AddOpenApi();
            builder.Services.AddLocalization();

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();

                //daha sonra kaldýrýlabilir
                options.ForwardedForHeaderName = "X-Real-IP";

                var knownNetworks = builder.Configuration["ForwardedHeadersOptions:KnownNetworks"];
                if (!string.IsNullOrEmpty(knownNetworks))
                {
                    var cidrParts = knownNetworks.Split('/');
                    if (cidrParts.Length == 2 && IPAddress.TryParse(cidrParts[0], out var ipAddress) && int.TryParse(cidrParts[1], out var prefixLength))
                        options.KnownNetworks.Add(new IPNetwork(ipAddress, prefixLength));
                }
                //options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("::ffff:172.22.0.0"), 112));
            });

            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? builder.Configuration["Redis:ConnectionString"];

                if (string.IsNullOrEmpty(redisConnectionString))
                {
                    throw new InvalidOperationException("Redis connection string 'Redis:ConnectionString' not found in configuration.");
                }

                var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);

                configurationOptions.AbortOnConnectFail = false;

                return ConnectionMultiplexer.Connect(configurationOptions);
            });

            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
            var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
            builder.Services.AddConsul(builder.Configuration);
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

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.TryGetValue("AccessToken", out var tokenFromCookie))
                            context.Token = tokenFromCookie;

                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            var supportedCultures = new[] { "en-US", "tr-TR" };
            app.UseRequestLocalization(new RequestLocalizationOptions()
                .SetDefaultCulture(supportedCultures[0])
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures));

            //app.UseHttpsRedirection();
            app.UseForwardedHeaders();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapGet("/health", () => Results.Ok());
            app.RegisterWithConsul(app.Lifetime);

            app.MapControllers();

            app.Run();
        }
    }
}
