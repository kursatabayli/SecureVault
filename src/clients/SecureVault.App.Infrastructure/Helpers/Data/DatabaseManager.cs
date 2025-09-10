using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureVault.App.Infrastructure.Context;

namespace SecureVault.App.Infrastructure.Helpers.Data
{
    public static class DatabaseManager
    {
        public static async Task EnsureDatabaseIsReadyAsync(IServiceProvider serviceProvider)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DatabaseManager");
            try
            {
                logger.LogInformation("Veritabanı durumu kontrol ediliyor ve hazırlanıyor...");
                var dbContext = services.GetRequiredService<SecureVaultDbContext>();
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Veritabanı hazır.");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Veritabanı hazırlığı sırasında kritik bir hata oluştu.");
                throw;
            }
        }

        public static async Task DeleteDatabaseAsync(IServiceProvider serviceProvider)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DatabaseManager");
            try
            {
                logger.LogInformation("Veritabanı silme işlemi başlatılıyor...");
                var dbContext = services.GetRequiredService<SecureVaultDbContext>();
                await dbContext.Database.EnsureDeletedAsync();
                logger.LogInformation("Veritabanı başarıyla silindi.");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Veritabanı silme işlemi sırasında kritik bir hata oluştu.");
                throw;
            }
        }

        public static async Task ResetDatabaseAsync(IServiceProvider serviceProvider)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DatabaseManager");
            try
            {
                logger.LogInformation("Veritabanı sıfırlama işlemi başlatılıyor...");
                var dbContext = services.GetRequiredService<SecureVaultDbContext>();

                await dbContext.Database.EnsureDeletedAsync();
                logger.LogInformation("Veritabanı başarıyla silindi.");

                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Veritabanı başarıyla yeniden oluşturuldu. Sıfırlama tamamlandı.");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Veritabanı sıfırlaması sırasında kritik bir hata oluştu.");
                throw;
            }
        }
    }
}
