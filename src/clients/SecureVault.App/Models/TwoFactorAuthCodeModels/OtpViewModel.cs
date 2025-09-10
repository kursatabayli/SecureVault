using OtpNet;

namespace SecureVault.App.Models.TwoFactorAuthCodeModels
{
    public class OtpViewModel
    {
        public TwoFactorAuthCodeModel Model { get; init; } = new();
        public string CurrentCode { get; set; } = "------";
        public bool HasError { get; set; } = false; 
        public int TimeLeft { get; set; }
        public double ProgressValue { get; set; }
        public Otp? OtpGenerator { get; set; }
        public bool IsCodeReady => !HasError && CurrentCode != "------";
    }
}
