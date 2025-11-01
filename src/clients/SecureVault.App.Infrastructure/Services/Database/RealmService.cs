using Realms;
using SecureVault.App.Application.Contracts.Abstractions.Database;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;

namespace SecureVault.App.Infrastructure.Services.Database
{
    public class RealmService : IRealmService
    {
        private readonly IStorageService _storageService;
        private Realm? _realm;
        private string? _dbPath;
        public RealmService(IStorageService storageService)
        {
            _storageService = storageService;
        }
        public async Task<Realm> GetRealmAsync()
        {
            if (_realm == null || _realm.IsClosed)
            {
                var config = await GetEncryptedConfigurationAsync();
                _realm = await Realm.GetInstanceAsync(config);
            }
            return _realm;
        }

        private async Task<RealmConfiguration> GetEncryptedConfigurationAsync()
        {
            var encryptionKey = await _storageService.GetOrCreateDbKeyAsync();

            if (encryptionKey == null)
                throw new InvalidOperationException("Could not create or retrieve the encryption key.");

            _dbPath = await _storageService.GetOrCreateDbPathAsync();

            return new RealmConfiguration(_dbPath)
            {
                EncryptionKey = encryptionKey,
                SchemaVersion = 2,
            };
        }

        public void Dispose()
        {
            _realm?.Dispose();
        }
    }
}
