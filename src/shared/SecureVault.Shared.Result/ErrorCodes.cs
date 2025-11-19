namespace SecureVault.Shared.Result;

public static class ErrorCodes
{
    public const string InternalServerError = "InternalServerError";
    public const string UnauthorizedAccess = "UnauthorizedAccess";
    public const string UnexpectedError = "UnexpectedError";
    public const string NotFound = "NotFound";
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

    public static class UserRecovery
    {
        public const string RecoveryDataAlreadyExists = "UserRecovery_RecoveryKeyAlreadyExists";
    }
    public static class Client
    {
        public const string NetworkError = "Network_Error";
        public const string LoadFailed = "Load_Failed";
        public const string SaveFailed = "Save_Failed";
        public const string SyncPullNoKey = "Sync_Pull_NoKey";
        public const string SyncPullCriticalError = "Sync_Pull_CriticalError";
        public const string SyncPull = "Sync_Pull";
    }
}
