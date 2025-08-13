namespace SecureVault.Identity.Domain.Entities
{
    public class UserSession
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string RefreshTokenJti { get; set; }
        public string AccessTokenJti { get; set; }
        public bool IsPersistent { get; private set; }
        public DeviceDetail DeviceDetails { get; private set; }
        public string? IpAddress { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? ExpiresAt { get; private set; }
        public bool IsRevoked { get; private set; }
        public DateTimeOffset? LastUsedAt { get; private set; }


        private UserSession() { }

        public static UserSession Create(Guid userId,
            string refreshTokenJti,
            string accessTokenJti,
            DeviceDetail deviceDetails,
            string? ipAddress,
            DateTimeOffset expiresAt,
            bool isRevoked,
            bool isPersistent)
        {
            var creationTime = DateTimeOffset.UtcNow;
            return new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RefreshTokenJti = refreshTokenJti,
                AccessTokenJti = accessTokenJti,
                DeviceDetails = deviceDetails,
                IpAddress = ipAddress,
                ExpiresAt = expiresAt,
                CreatedAt = creationTime,
                LastUsedAt = creationTime,
                IsRevoked = isRevoked,
                IsPersistent = isPersistent
            };
        }
        public void Update(string refreshTokenJti,
            string accessTokenJti,
            string? ipAddress,
            DateTimeOffset expiresAt,
            bool isPersistent = true)
        {
            RefreshTokenJti = refreshTokenJti;
            AccessTokenJti = accessTokenJti;
            IpAddress = ipAddress;
            ExpiresAt = expiresAt;
            LastUsedAt = DateTimeOffset.UtcNow;
            IsRevoked = false;
            IsPersistent = isPersistent;
        }
        public void Revoke()
        {
            if (!IsRevoked)
                IsRevoked = true;
        }
    }
}
