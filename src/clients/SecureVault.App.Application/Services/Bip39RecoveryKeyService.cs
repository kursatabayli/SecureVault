using dotnetstandard_bip39;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;

namespace SecureVault.App.Application.Services
{
    public class Bip39RecoveryKeyService : IBip39RecoveryKeyService
    {
        private readonly ICryptoService _cryptoService;
        private readonly BIP39 _bip39;

        public Bip39RecoveryKeyService(ICryptoService cryptoService)
        {
            _bip39 = new BIP39();
            _cryptoService = cryptoService;
        }

        public string Generate()
        {
            const int entropyBits = 256;

            return _bip39.GenerateMnemonic(entropyBits, BIP39Wordlist.English);
        }

        public bool Validate(string mnemonic) => _bip39.ValidateMnemonic(mnemonic, BIP39Wordlist.English);

        public byte[] BackupEncryptionKey(string mnemonic, byte[] encryptionKey)
        {
            string seedHex = _bip39.MnemonicToSeedHex(mnemonic, "");

            byte[] seed = Convert.FromHexString(seedHex);

            var recoveryEncryptionKey = new byte[32];
            Buffer.BlockCopy(seed, 0, recoveryEncryptionKey, 0, 32);

            byte[] encryptedMainKey = _cryptoService.EncryptBytes(encryptionKey, recoveryEncryptionKey);

            Array.Clear(seed, 0, seed.Length);
            Array.Clear(encryptionKey, 0, encryptionKey.Length);
            Array.Clear(recoveryEncryptionKey, 0, recoveryEncryptionKey.Length);

            return encryptedMainKey;
        }
    }
}