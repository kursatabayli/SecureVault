namespace SecureVault.App.Application.Contracts.DTOs.Passwords
{
    public class PasswordDto
    {
        public string SiteName { get; set; } = string.Empty;
        public string SiteUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
