using Microsoft.EntityFrameworkCore;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Infrastructure.Context;

namespace SecureVault.Identity.Infrastructure.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task SaveChangesWithTransactionAsync()
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();

            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async ValueTask DisposeAsync()
        {
            if (_context != null)
                await _context.DisposeAsync();
        }

    }
}

