namespace SecureVault.Identity.Domain.Entites
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string PublicKey { get; set; } 
        public UserInfo? UserInfo { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

    }
}
