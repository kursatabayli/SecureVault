using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;
using SecureVault.App.Infrastructure.Context;

namespace SecureVault.App.Infrastructure.Repositories
{
    public class PasswordRepository : GenericRepository<PasswordEntity>, IPasswordRepository
    {
        public PasswordRepository(SecureVaultDbContext context) : base(context)
        {
        }
    }
}
