namespace SecureVault.App.Application.Contracts.Abstractions.Sync;

public interface ISyncProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken);
}
