using System.Text.Json.Serialization;

namespace SecureVault.App.Services.Models.VaultItemModels
{
    public class TwoFactorAuthModel : IVaultItemData
    {
        [JsonIgnore]
        public Guid Id { get; set; }
        public string Issuer { get; set; }
        public string AccountName { get; set; }
        public string SecretKey { get; set; }
        public OtpType Type { get; set; } = OtpType.TOTP;
        public int Digits { get; set; } = 6;
        public int Period { get; set; } = 30;
        public long Counter { get; set; } = 0;
        public OtpAlgorithm Algorithm { get; set; } = OtpAlgorithm.SHA1;
        [JsonIgnore]
        public DateTime CreatedAt { get; set; }
    }

    public enum OtpType
    {
        TOTP,
        HOTP
    }

    public enum OtpAlgorithm
    {
        SHA1,
        SHA256,
        SHA512
    }
}
