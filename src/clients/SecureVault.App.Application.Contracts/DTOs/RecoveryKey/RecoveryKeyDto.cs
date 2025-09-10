namespace SecureVault.App.Application.Contracts.DTOs.RecoveryKey
{
    public class RecoveryKeyDto
    {
        public string Mnemonic { get; }
        public string[] Words => Mnemonic.Split(' ');
        public RecoveryKeyDto(string mnemonic)
        {
            if (string.IsNullOrWhiteSpace(mnemonic))
            {
                throw new ArgumentException("Mnemonic cannot be null or whitespace.", nameof(mnemonic));
            }
            Mnemonic = mnemonic;
        }
    }
}
