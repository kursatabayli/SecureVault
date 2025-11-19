using SecureVault.App.Application.Contracts.DTOs.Sync;
using SecureVault.App.Domain.Entities;

namespace SecureVault.App.Application.Contracts.Abstractions.Sync;

public interface IEntityPayloadFactory<in TEntity> where TEntity : ISynchronizableEntity
{
    SyncPayload CreatePayload(TEntity entity);
}
