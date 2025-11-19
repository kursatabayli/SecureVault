namespace SecureVault.App.Models.PassowordModels;

public class CreatePasswordModel
{
    public string SiteName { get; set; }
    public string SiteUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string? Notes { get; set; }
}
