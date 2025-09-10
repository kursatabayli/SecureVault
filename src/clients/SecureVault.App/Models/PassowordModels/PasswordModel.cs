namespace SecureVault.App.Models.PassowordModels
{
    public class PasswordModel
    {
        public Guid Id { get; set; }
        public string SiteName { get; set; }
        public string SiteUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string? Notes { get; set; }
        public int Version { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsSynced { get; set; }
    }
}
