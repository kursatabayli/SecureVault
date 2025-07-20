namespace SecureVault.Identity.Application.Contracts.Services
{
    public interface IHashService
    {
        (string Hash, string Salt) HashItem(string Item);
        bool VerifyItem(string requestItem, string hashedItem, string saltItem);
    }
}
