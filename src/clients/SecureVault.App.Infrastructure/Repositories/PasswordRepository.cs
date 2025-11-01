using SecureVault.App.Application.Contracts.Abstractions.Database;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;

namespace SecureVault.App.Infrastructure.Repositories
{
    public class PasswordRepository : GenericRepository<PasswordEntity>, IPasswordRepository
    {
        public PasswordRepository(IRealmService realmService) : base(realmService)
        {
        }
    }
}
