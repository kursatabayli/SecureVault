namespace SecureVault.Shared.Result
{
    public static class ErrorCodes
    {
        public const string InternalServerError = "InternalServerError";
        public const string UnauthorizedAccess = "UnauthorizedAccess";

        public static class Auth
        {
            public const string LoginFailed = "Auth_LoginFailed";
            public const string UserNotFound = "Auth_UserNotFound";
            public const string ChallengeExpired = "Auth_ChallengeExpired";
            public const string InvalidSignature = "Auth_InvalidSignature";
            public const string InvalidRefreshToken = "Auth_InvalidRefreshToken";
            public const string EmailInUse = "Auth_EmailInUse";
        }

        public static class Vault
        {
            public const string ItemNotFound = "Vault_ItemNotFound";
        }

        public static class Client
        {
            public const string NetworkError = "NetworkError";
            public const string LoadFailed = "LoadFailed";
        }
    }
}
