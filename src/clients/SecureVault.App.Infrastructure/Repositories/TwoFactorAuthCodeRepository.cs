using SecureVault.App.Application.Contracts.Abstractions.Database;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;

namespace SecureVault.App.Infrastructure.Repositories;

public class TwoFactorAuthCodeRepository : GenericRepository<TwoFactorAuthCodeEntity>, ITwoFactorAuthCodeRepository
{
    public TwoFactorAuthCodeRepository(IRealmService realmService) : base(realmService)
    {
    }
}
