using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using SecureVault.Vault.Api.Extensions;
using SecureVault.Vault.Application;
using System.Net;
using System.Text;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;
namespace SecureVault.Vault.Api
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
            builder.Services.AddConsul(builder.Configuration);


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            //app.UseHttpsRedirection();
            app.UseForwardedHeaders();

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapGet("/health", () => Results.Ok());
            app.RegisterWithConsul(app.Lifetime);

            app.MapControllers();

            app.Run();
        }
    }
}
