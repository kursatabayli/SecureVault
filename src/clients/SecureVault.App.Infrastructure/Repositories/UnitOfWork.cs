using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Infrastructure.Context;

namespace SecureVault.App.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SecureVaultDbContext _context; 
        private readonly Dictionary<Type, object> _repositories = [];
        private IPasswordRepository _passwords;
        private ITwoFactorAuthCodeRepository _twoFactorAuthCodes;

        public UnitOfWork(SecureVaultDbContext context)
        {
            _context = context;
        }

        public IPasswordRepository Passwords => _passwords ??= new PasswordRepository(_context);
        public ITwoFactorAuthCodeRepository TwoFactorAuthCodes => _twoFactorAuthCodes ??= new TwoFactorAuthCodeRepository(_context);
        public IRepository<T> GetRepository<T>() where T : class
        {
            var type = typeof(T);
            if (_repositories.ContainsKey(type))
            {
                return (IRepository<T>)_repositories[type];
            }

            var repository = new GenericRepository<T>(_context);
            _repositories.Add(type, repository);
            return repository;
        }

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();
        public async ValueTask DisposeAsync() => await _context.DisposeAsync();
    }
}
