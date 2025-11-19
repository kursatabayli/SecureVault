using Realms;

namespace SecureVault.App.Application.Contracts.Abstractions.Database;

public interface IRealmService : IDisposable
{
    Task<Realm> GetRealmAsync();

}
