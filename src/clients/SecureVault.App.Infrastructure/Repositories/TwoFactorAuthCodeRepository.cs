using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;
using SecureVault.App.Infrastructure.Context;

namespace SecureVault.App.Infrastructure.Repositories
{
    public class TwoFactorAuthCodeRepository : GenericRepository<TwoFactorAuthCodeEntity>, ITwoFactorAuthCodeRepository
    {
        public TwoFactorAuthCodeRepository(SecureVaultDbContext context) : base(context)
        {
        }
    }
}
