using SecureVault.App.Domain.Entities;

namespace SecureVault.App.Application.Contracts.Repositories;

public interface ITwoFactorAuthCodeRepository : IRepository<TwoFactorAuthCodeEntity>
{
}
