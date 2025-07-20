namespace SecureVault.ApiGateway.Helpers
{
    public class JwtSettings
    {
        public string Key { get; set; }
        public string RefreshTokenKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
    }
}
