﻿using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services.Service.Contracts
{
    public interface IVaultItemService<T> where T : class
    {
        Task<Result<IReadOnlyCollection<T>>> GetVaultItemsByItemTypeAsync(ItemType itemType);
        Task<bool> CreateVaultItem(T Data, ItemType itemType);
    }
}
