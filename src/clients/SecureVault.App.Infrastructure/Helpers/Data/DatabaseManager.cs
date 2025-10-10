using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureVault.App.Infrastructure.Context;

namespace SecureVault.App.Infrastructure.Helpers.Data
{
    public class DatabaseManager
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILoggerFactory _loggerFactory;

        // Constructor ile IServiceScopeFactory ve ILoggerFactory enjekte ediyoruz.
        public DatabaseManager(IServiceScopeFactory scopeFactory, ILoggerFactory loggerFactory)
        {
            _scopeFactory = scopeFactory;
            _loggerFactory = loggerFactory;
        }
        public async Task EnsureDatabaseIsReadyAsync()
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var logger = _loggerFactory.CreateLogger("DatabaseManager");
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

        public async Task DeleteDatabaseAsync()
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var logger = _loggerFactory.CreateLogger("DatabaseManager");
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

        public async Task ResetDatabaseAsync()
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var logger = _loggerFactory.CreateLogger("DatabaseManager");
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
