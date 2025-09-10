using System.ComponentModel.DataAnnotations;

namespace SecureVault.App.Domain.Entities
{
    public class PasswordEntity : ISynchronizableEntity
    {
        [Key]
        public Guid Id { get; private set; }
        public string SiteName { get; private set; }
        public string SiteUrl { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string? Notes { get; private set; }
        public int Version { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public bool IsSynced { get; private set; }
        public PasswordEntity() { }

        public static PasswordEntity Create(
            Guid? id,
            string siteName,
            string siteUrl,
            string username,
            string password,
            string? notes,
            int? version,
            DateTimeOffset? createdAt,
            DateTimeOffset? updatedAt)
        {
            var actualCreatedAt = createdAt ?? DateTimeOffset.UtcNow;
            return new PasswordEntity
            {
                Id = id ?? Guid.NewGuid(),
                SiteName = siteName,
                SiteUrl = siteUrl,
                Username = username,
                Password = password,
                Notes = notes,
                Version = version ?? 1,
                CreatedAt = actualCreatedAt,
                UpdatedAt = updatedAt ?? actualCreatedAt,
                IsDeleted = false,
                IsSynced = false
            };
        }

        public void Update(
            string siteName,
            string siteUrl,
            string username,
            string password,
            string? notes,
            int version,
            DateTimeOffset updatedAt)
        {
            SiteName = siteName;
            SiteUrl = siteUrl;
            Username = username;
            Password = password;
            Notes = notes;
            Version = version;
            UpdatedAt = updatedAt;
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
}
