namespace SecureVault.App.Services.Constants
{
    public static class Endpoints
    {
        public static readonly string BaseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "https://192.168.31.244:7202/" : "https://localhost:7202/";

        public static readonly string AuthBaseUrl = BaseUrl + "identity/api/Auth/";
        public static readonly string AuthAuhtorizeUrl = AuthBaseUrl + "authorize";
        public static readonly string ChallengeUrl = AuthBaseUrl + "challenge/";
        public static readonly string LoginUrl = AuthBaseUrl + "login?rememberMe={0}";
        public static readonly string RefreshTokenUrl = AuthBaseUrl + "refresh/";
        public static readonly string LogoutUrl = AuthBaseUrl + "logout/";

        public static readonly string RegisterBaseUrl = BaseUrl + "identity/api/Register/";

        public static readonly string VaultItemBaseUrl = BaseUrl + "vault/api/VaultItem/";
        public static readonly string GetVaultItemsByItemTypeUrl = VaultItemBaseUrl + "vault-items/type/";
        public static readonly string IsHealthyUrl = VaultItemBaseUrl + "is-healthy";
    }
}
