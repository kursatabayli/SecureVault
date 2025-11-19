using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Infrastructure.Context;

namespace SecureVault.Identity.Infrastructure.Services;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(AppDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Changes saved successfully (non-transactional).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save changes (non-transactional).");
            throw;
        }
    }

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
                _logger.LogInformation("Transaction committed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed and is being rolled back.");
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

