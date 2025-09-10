using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Application.Services;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Sync
{
    public class SyncDataProcessor : ISyncDataProcessor
    {
        private readonly IReadOnlyDictionary<ItemType, IEntityDataProcessor> _processors;

        public SyncDataProcessor(IEnumerable<IEntityDataProcessor> processors)
        {
            _processors = processors.ToDictionary(p => p.Type);
        }

        public async Task<Result> ProcessServerDataAsync(IReadOnlyCollection<VaultItemDto> serverItems, IUnitOfWork unitOfWork)
        {
            foreach (var item in serverItems)
            {
                if (_processors.TryGetValue(item.ItemType, out var processor))
                {
                    await processor.ProcessItemAsync(unitOfWork, item);
                }
            }
            await unitOfWork.CompleteAsync();
            await UINotificationService.NotifyVaultDataChanged();
            return Result.Success();
        }
    }
}
