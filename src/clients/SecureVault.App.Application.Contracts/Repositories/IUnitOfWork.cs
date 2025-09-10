namespace SecureVault.App.Application.Contracts.Repositories
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IPasswordRepository Passwords { get; }
        ITwoFactorAuthCodeRepository TwoFactorAuthCodes { get; }
        IRepository<T> GetRepository<T>() where T : class;
        Task<int> CompleteAsync();
    }
}
