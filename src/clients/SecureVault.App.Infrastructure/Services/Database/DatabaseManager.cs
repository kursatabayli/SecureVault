using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Database;

namespace SecureVault.App.Infrastructure.Services.Database;

public class DatabaseManager : IDatabaseManager
{
  private readonly ILogger<DatabaseManager> _logger;
  public DatabaseManager(ILogger<DatabaseManager> logger)
  {
    _logger = logger;
  }
  public async Task CleanupOldDbFilesAsync()
  {

    _logger.LogInformation("Starting old database file cleanup task...");

    try
    {
      string appDataDir = FileSystem.Current.AppDataDirectory;
      string searchPattern = "securevaultdb-*.realm*";

      await Task.Run(() =>
              {
                var fileSystemEntries = Directory.GetFileSystemEntries(appDataDir, searchPattern);

                if (!fileSystemEntries.Any())
                {
                  _logger.LogInformation("No old database files/folders found to clean up.");
                  return;
                }

                _logger.LogInformation("Found {Count} old database files/folders to delete...", fileSystemEntries.Length);

                foreach (var entryPath in fileSystemEntries)
                {
                  try
                  {
                    var attributes = File.GetAttributes(entryPath);

                    if (attributes.HasFlag(FileAttributes.Directory))
                    {
                      Directory.Delete(entryPath, recursive: true);
                      _logger.LogInformation("Deleted old database folder: {Path}", entryPath);
                    }
                    else
                    {
                      File.Delete(entryPath);
                      _logger.LogInformation("Deleted old database file: {Path}", entryPath);
                    }
                  }
                  catch (IOException ioEx)
                  {
                    _logger.LogWarning(ioEx, "Could not delete item (may be locked): {Path}", entryPath);
                  }
                  catch (UnauthorizedAccessException uaEx)
                  {
                    _logger.LogWarning(uaEx, "Access denied for item: {Path}", entryPath);
                  }
                  catch (Exception ex)
                  {
                    _logger.LogWarning(ex, "Error processing item: {Path}", entryPath);
                  }
                }
              });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "A general error occurred during the database cleanup process.");
    }
  }
}
