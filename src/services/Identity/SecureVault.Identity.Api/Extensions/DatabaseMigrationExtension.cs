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

    logger.LogInformation("Applying database migrations (Attempt 1/{MaxRetries})...", maxRetries);

    while (attempt < maxRetries)
    {
      try
      {
        dbContext.Database.Migrate();
        logger.LogInformation("Database migration completed successfully.");
        break;
      }
      catch (Exception ex)
      {
        attempt++;
        logger.LogWarning(ex, "Database migration failed (Attempt: {Attempt}/{MaxRetries}). Waiting {Delay}ms...", attempt, maxRetries, delayMilliseconds);

        if (attempt >= maxRetries)
        {
          logger.LogError("Maximum retry attempts reached. Database migration failed.");
          throw;
        }

        Thread.Sleep(delayMilliseconds);
      }
    }
  }
}
