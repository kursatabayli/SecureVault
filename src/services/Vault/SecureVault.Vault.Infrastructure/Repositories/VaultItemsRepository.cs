using Microsoft.EntityFrameworkCore;
using SecureVault.Vault.Application.Contracts.Repositories;
using SecureVault.Vault.Domain.Entities;
using SecureVault.Vault.Domain.Enums;
using SecureVault.Vault.Infrastructure.Context;

namespace SecureVault.Vault.Infrastructure.Repositories
{
    public class VaultItemsRepository : IVaultItemsRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<VaultItem> _dbSet;

        public VaultItemsRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<VaultItem>();
        }
        public async Task<VaultItem?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);
        public async Task AddAsync(VaultItem vaultItem) => await _dbSet.AddAsync(vaultItem);
        public IQueryable<VaultItem> GetAllUserVaultItemsByVaultTypeAsync(Guid userId, ItemType itemType) 
            => _context.VaultItems.Where(v => v.UserId == userId && v.ItemType == itemType && !v.IsDeleted);

    }
}
