namespace SecureVault.App.Models.PassowordModels
{
    public class PasswordViewModel
    {
        public PasswordModel Model { get; init; } = new();
        public bool IsPasswordVisible { get; set; } = false;
        public string DisplayPassword => IsPasswordVisible ? Model.Password : "••••••••";
    }
}
