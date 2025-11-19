namespace SecureVault.App.Models.RecoveryKeyModels;

public class RecoveryKeyModel
{
    public string Mnemonic { get; }
    public string[] Words => Mnemonic.Split(' ');
    public RecoveryKeyModel(string mnemonic)
    {
        if (string.IsNullOrWhiteSpace(mnemonic))
        {
            throw new ArgumentException("Mnemonic cannot be null or whitespace.", nameof(mnemonic));
        }
        Mnemonic = mnemonic;
    }
}
