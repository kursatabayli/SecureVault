using System.Linq.Expressions;

namespace SecureVault.Vault.Application.Contracts.Specifications;

public interface ISpecification<TEntity, TResult>
{
    Expression<Func<TEntity, bool>> Criteria { get; }
    Expression<Func<TEntity, TResult>> Projection { get; }
}
