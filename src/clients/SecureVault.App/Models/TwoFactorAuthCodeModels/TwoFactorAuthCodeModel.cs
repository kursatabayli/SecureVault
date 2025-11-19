using SecureVault.App.Domain.Enums;

namespace SecureVault.App.Models.TwoFactorAuthCodeModels;

public class TwoFactorAuthCodeModel
{
    public Guid Id { get; set; }
    public string Issuer { get; set; }
    public string AccountName { get; set; }
    public string SecretKey { get; set; }
    public OtpType Type { get; set; } = OtpType.TOTP;
    public int Digits { get; set; } = 6;
    public int Period { get; set; } = 30;
    public long Counter { get; set; } = 0;
    public OtpAlgorithm Algorithm { get; set; } = OtpAlgorithm.SHA1;
}
