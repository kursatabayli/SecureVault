using MongoDB.Driver;

namespace SecureVault.Vault.Api.Extensions;

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
                      throw new InvalidOperationException("'MongoDbSettings:DatabaseName' is not configured. " + "Ensure this setting is added to appsettings.json or the AppHost.");
                  }

                  return client.GetDatabase(dbName);
              });

        return services;
    }
}
