using Microsoft.Extensions.Logging;
using Realms;
using SecureVault.App.Application.Contracts.Abstractions.Database;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;

namespace SecureVault.App.Infrastructure.Services.Database;

public class RealmService : IRealmService
{
    private readonly IStorageService _storageService;
    private readonly ILogger<RealmService> _logger;
    private Realm? _realm;
    private string? _dbPath;

    private readonly SemaphoreSlim _realmLock = new(1, 1);

    public RealmService(IStorageService storageService, ILogger<RealmService> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }
    public async Task<Realm> GetRealmAsync()
    {
        if (_realm != null && !_realm.IsClosed)
        {
            _logger.LogTrace("Returning existing (cached) Realm instance.");
            return _realm;
        }

        await _realmLock.WaitAsync();
        try
        {
            if (_realm != null && !_realm.IsClosed)
            {
                _logger.LogTrace("Returning existing Realm instance (acquired via double-check lock).");
                return _realm;
            }

            _logger.LogInformation("Realm instance is null or closed. Initializing new Realm instance...");
            var config = await GetEncryptedConfigurationAsync();

            _logger.LogInformation("Opening Realm instance at path: {DbPath}", config.DatabasePath);
            _realm = await Realm.GetInstanceAsync(config);
            _logger.LogInformation("New Realm instance opened successfully.");

            return _realm;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to initialize Realm instance. The application cannot proceed.");
            throw;
        }
        finally
        {
            _realmLock.Release();
        }
    }

    private async Task<RealmConfiguration> GetEncryptedConfigurationAsync()
    {
        _logger.LogDebug("Building RealmConfiguration: Getting DB encryption key...");
        var encryptionKey = await _storageService.GetOrCreateDbKeyAsync();

        if (encryptionKey == null)
        {
            _logger.LogCritical("Could not create or retrieve the Realm encryption key. This is fatal.");
            throw new InvalidOperationException("Could not create or retrieve the encryption key.");
        }

        _logger.LogDebug("Building RealmConfiguration: Getting DB path...");
        _dbPath = await _storageService.GetOrCreateDbPathAsync();

        if (string.IsNullOrEmpty(_dbPath))
        {
            _logger.LogCritical("Could not create or retrieve the Realm DB path. This is fatal.");
            throw new InvalidOperationException("Could not create or retrieve the DB path.");
        }

        _logger.LogDebug("RealmConfiguration built successfully.");
        return new RealmConfiguration(_dbPath)
        {
            EncryptionKey = encryptionKey,
            SchemaVersion = 2,
        };
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing Realm instance and lock...");
        _realm?.Dispose();
        _realmLock?.Dispose();
    }
}
