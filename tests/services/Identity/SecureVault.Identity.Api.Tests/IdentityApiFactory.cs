using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using SecureVault.Identity.Infrastructure.Context;
using StackExchange.Redis;
using System.Text;

namespace SecureVault.Identity.Api.Tests
{
    public class IdentityApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();


                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"IdentityDbForTesting_{Guid.NewGuid()}");
                });


                services.RemoveAll<IConnectionMultiplexer>();
                var redisMock = new Mock<IConnectionMultiplexer>();
                var databaseMock = new Mock<IDatabase>();
                redisMock.Setup(_ => _.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(databaseMock.Object);
                services.AddSingleton(redisMock.Object);

                services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKeyForTestingPurposes123!")),
                        ValidateIssuer = true,
                        ValidIssuer = "TestIssuer",
                        ValidateAudience = true,
                        ValidAudience = "TestAudience",
                        ValidateLifetime = false
                    };
                });
            });
            builder.UseEnvironment("Testing");

        }
    }
}

