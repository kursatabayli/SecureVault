namespace SecureVault.Identity.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; }
        public byte[] PublicKey { get; private set; }
        public byte[] Salt { get; private set; }
        public UserInfo UserInfo { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }

        private User() { }

        public static User Create(string email, byte[] publicKey, byte[] salt, UserInfo userInfo)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Email = email.ToLowerInvariant(),
                PublicKey = publicKey,
                Salt = salt,
                UserInfo = userInfo,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }
        public void UpdateUserInfo(UserInfo userInfo)
        {
            UserInfo = userInfo;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
