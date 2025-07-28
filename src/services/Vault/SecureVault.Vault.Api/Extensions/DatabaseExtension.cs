using MongoDB.Driver;

namespace SecureVault.Vault.Api.Extensions
{
    public static class DatabaseExtension
    {
        public static IServiceCollection AddMongoDbConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var mongoDbSettings = configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();

            services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoDbSettings.ConnectionString));

            services.AddScoped<IMongoDatabase>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                return client.GetDatabase(mongoDbSettings.DatabaseName);
            });

            return services;
        }
    }

    public class MongoDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
}
