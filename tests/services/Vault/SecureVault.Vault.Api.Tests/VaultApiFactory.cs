using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Mongo2Go;
using MongoDB.Driver;
using System.Text;

namespace SecureVault.Vault.Api.Tests
{
    public class VaultApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private MongoDbRunner _mongoDbRunner = null!;
        public IMongoDatabase Database { get; private set; } = null!;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMongoClient>();
                services.RemoveAll<IMongoDatabase>();

                services.AddSingleton(Database);


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
        }

        public Task InitializeAsync()
        {
            _mongoDbRunner = MongoDbRunner.Start();
            var client = new MongoClient(_mongoDbRunner.ConnectionString);
            Database = client.GetDatabase("api_test_db");
            return Task.CompletedTask;
        }

        public new Task DisposeAsync()
        {
            _mongoDbRunner.Dispose();
            return Task.CompletedTask;
        }
    }
}