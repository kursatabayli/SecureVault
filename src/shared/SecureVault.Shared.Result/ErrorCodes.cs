namespace SecureVault.Shared.Result
{
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

        public static class Sync
        {
            public const string SyncDateOld = "Sync_DateOld";
            public const string SyncConflict = "Sync_Conflict";
            public const string SyncDisabled = "Sync_Disabled";
            public const string SyncAlreadyInProgress = "Sync_AlreadyInProgress";
        }

        public static class Client
        {
            public const string NetworkError = "NetworkError";
            public const string LoadFailed = "LoadFailed";
        }
    }
}
