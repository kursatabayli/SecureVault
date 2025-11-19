using SecureVault.App.Domain.Enums;

namespace SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Results;

public class TwoFactorAuthCodeResult
{
    public Guid Id { get; set; }
    public string Issuer { get; set; }
    public string AccountName { get; set; }
    public string SecretKey { get; set; }
    public OtpType Type { get; set; }
    public int Digits { get; set; }
    public int Period { get; set; }
    public long Counter { get; set; }
    public OtpAlgorithm Algorithm { get; set; }
    public int Version { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsSynced { get; set; }
}
