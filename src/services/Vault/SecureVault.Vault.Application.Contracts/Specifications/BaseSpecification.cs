using System.Linq.Expressions;

namespace SecureVault.Vault.Application.Contracts.Specifications
{
    public abstract class BaseSpecification<TEntity, TResult> : ISpecification<TEntity, TResult>
    {
        public Expression<Func<TEntity, bool>> Criteria { get; protected set; }
        public Expression<Func<TEntity, TResult>> Projection { get; protected set; }
    }
}
