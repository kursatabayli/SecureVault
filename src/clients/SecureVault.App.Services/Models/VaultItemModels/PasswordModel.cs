using System.Text.Json.Serialization;

namespace SecureVault.App.Services.Models.VaultItemModels
{
    public class PasswordModel : IVaultItemData
    {
        [JsonIgnore]
        public Guid Id { get; set; }

        public string SiteName { get; set; } = string.Empty;

        public string SiteUrl { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string? Notes { get; set; }

        [JsonIgnore]
        public DateTime CreatedAt { get; set; }
    }
}
