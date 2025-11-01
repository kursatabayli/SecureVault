namespace SecureVault.App.Application.Contracts.Abstractions.Database;

public interface IDatabaseManager
{
  Task CleanupOldDbFilesAsync();
}
