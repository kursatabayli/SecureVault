using Microsoft.EntityFrameworkCore;
using Npgsql;
using SecureVault.Identity.Infrastructure.Context;

namespace SecureVault.Identity.Api.Extensions;

public static class DatabaseMigrationExtension
{
  public static void ApplyDatabaseMigrations(this WebApplication app)
  {
    var maxRetries = 10;
    var delayMilliseconds = 5000;
    var attempt = 0;

    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<AppDbContext>();
    var logger = services.GetRequiredService<ILogger<WebApplication>>();

    while (attempt < maxRetries)
    {
      try
      {
        dbContext.Database.Migrate();
        logger.LogInformation("Veritabanı migration işlemi başarıyla tamamlandı.");
        break;
      }
      catch (NpgsqlException ex)
      {
        attempt++;
        logger.LogWarning(ex, "Veritabanı bağlantı hatası (Deneme: {Attempt}/{MaxRetries}). {Delay}ms bekleniyor...", attempt, maxRetries, delayMilliseconds);

        if (attempt >= maxRetries)
        {
          logger.LogError("Maksimum deneme sayısına ulaşıldı. Veritabanı migration işlemi başarısız.");
          throw;
        }

        Task.Delay(delayMilliseconds).Wait();
      }
    }
  }
}
