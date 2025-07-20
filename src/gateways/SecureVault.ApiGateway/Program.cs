using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using SecureVault.ApiGateway.Extensions;
using System.Net;
using System.Text;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace SecureVault.ApiGateway
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
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
                    ValidIssuer = builder.Configuration["JwtSettings:ValidIssuer"],
                    ValidAudience = builder.Configuration["JwtSettings:ValidAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"])),
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
            
            builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

            builder.Services.AddOcelot(builder.Configuration).AddConsul<MyConsulServiceBuilder>();

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

            app.UseForwardedHeaders();

            app.UseHttpsRedirection();
            app.UseCors("SecureVaultApp");
            app.UseAuthentication();
            app.UseAuthorization();
            await app.UseOcelot();

            await app.RunAsync();
        }
    }
}
