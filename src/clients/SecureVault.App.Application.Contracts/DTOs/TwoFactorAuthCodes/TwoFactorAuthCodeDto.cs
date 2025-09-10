using SecureVault.App.Domain.Enums;

namespace SecureVault.App.Application.Contracts.DTOs.TwoFactorAuthCodes
{
    public class TwoFactorAuthCodeDto
    {
        public string Issuer { get; set; }
        public string AccountName { get; set; }
        public string SecretKey { get; set; }
        public OtpType Type { get; set; }
        public int Digits { get; set; }
        public int Period { get; set; }
        public long Counter { get; set; }
        public OtpAlgorithm Algorithm { get; set; }
    }
}
