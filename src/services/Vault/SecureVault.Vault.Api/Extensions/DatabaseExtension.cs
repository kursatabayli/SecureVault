using MongoDB.Driver;

namespace SecureVault.Vault.Api.Extensions
{
    public static class DatabaseExtension
    {
        public static IServiceCollection AddMongoDbConfiguration(this IServiceCollection services, IHostApplicationBuilder builder)
        {
            builder.AddMongoDBClient("vault-db");

            services.AddScoped(sp =>
                  {
                      var client = sp.GetRequiredService<IMongoClient>();

                      var dbName = builder.Configuration.GetValue<string>("MongoDbSettings:DatabaseName");

                      if (string.IsNullOrEmpty(dbName))
                      {
                          throw new InvalidOperationException("'MongoDbSettings:DatabaseName' ayarı yapılandırılmamış. " + "Bunu appsettings.json'a veya AppHost'a eklediğinizden emin olun.");
                      }

                      return client.GetDatabase(dbName);
                  });

            return services;
        }
    }
}
