using System.Diagnostics;
using SecureVault.App.Application.Contracts.Abstractions.Database;

namespace SecureVault.App.Infrastructure.Services.Database
{
  public class DatabaseManager : IDatabaseManager
  {
    public async Task CleanupOldDbFilesAsync()
    {
      try
      {
        string appDataDir = FileSystem.Current.AppDataDirectory;
        string searchPattern = "securevaultdb-*.realm*";

        var fileSystemEntries = await Task.Run(() => Directory.GetFileSystemEntries(appDataDir, searchPattern));

        if (!fileSystemEntries.Any())
        {
          Debug.WriteLine("Temizlenecek eski veritabanı dosyası/klasörü bulunamadı.");
          return;
        }

        await Task.Run(() =>
        {
          foreach (var entryPath in fileSystemEntries)
          {
            try
            {
              var attributes = File.GetAttributes(entryPath);

              if (attributes.HasFlag(FileAttributes.Directory))
              {
                Directory.Delete(entryPath, recursive: true);
                Debug.WriteLine($"Eski veritabanı klasörü silindi: {entryPath}");
              }
              else
              {
                File.Delete(entryPath);
                Debug.WriteLine($"Eski veritabanı dosyası silindi: {entryPath}");
              }
            }
            catch (IOException ioEx)
            {
              Debug.WriteLine($"Öğe silinemedi (kilitli olabilir): {entryPath}, Hata: {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
              Debug.WriteLine($"Öğeye erişim reddedildi: {entryPath}, Hata: {uaEx.Message}");
            }
            catch (Exception ex)
            {
              Debug.WriteLine($"Öğe işlenirken hata: {entryPath}, Hata: {ex.Message}");
            }
          }
        });
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Veritabanı temizleme sırasında genel hata: {ex.Message}");
      }
    }
  }
}
