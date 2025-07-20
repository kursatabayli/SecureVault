using OtpNet;

namespace SecureVault.App.Services.Models.VaultItemModels
{
    public class TotpViewModel
    {
        public TwoFactorAuthModel Model { get; init; } = new();
        public string CurrentCode { get; set; } = "------";
        public bool HasError { get; set; } = false; 
        public int TimeLeft { get; set; }
        public double ProgressValue { get; set; }
        public Totp? TotpGenerator { get; set; }

        public bool IsCodeReady => !HasError && CurrentCode != "------";
    }
}
