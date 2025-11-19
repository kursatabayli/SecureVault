using Realms;

namespace SecureVault.App.Domain.Entities;

public partial class PasswordEntity : IRealmObject, ISynchronizableEntity
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string SiteUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsSynced { get; set; }
    public PasswordEntity()
    {
        Id = Guid.NewGuid();
        Version = 1;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
        IsDeleted = false;
        IsSynced = false;
    }


    public void MarkAsDeleted()
    {
        IsDeleted = true;
        IsSynced = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsSynced()
    {
        IsSynced = true;
    }
}
