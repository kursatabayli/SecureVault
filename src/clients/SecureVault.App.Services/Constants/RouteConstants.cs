namespace SecureVault.App.Services.Constants
{
    public static class RouteConstants
    {
        public const string Home = "/";
        public const string Login = "/login";
        public const string Register = "/register";
        public const string Logout = "/logout";
        public const string Profile = "/profile";
        public const string Vaults = "/vaults";
        public const string VaultDetails = "/vaults/{id}";
        public const string Items = "/items";
        public const string ItemDetails = "/items/{id}";
        public const string Settings = "/settings";
        public const string NotFound = "/not-found";
        public const string TwoFactor = "/two-factor";
    }
}
