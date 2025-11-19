using dotnetstandard_bip39;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;

namespace SecureVault.App.Application.Services;

public class Bip39RecoveryKeyService : IBip39RecoveryKeyService
{
    private readonly ICryptoService _cryptoService;
    private readonly ILogger<Bip39RecoveryKeyService> _logger;
    private readonly BIP39 _bip39;

    public Bip39RecoveryKeyService(ICryptoService cryptoService, ILogger<Bip39RecoveryKeyService> logger)
    {
        _cryptoService = cryptoService;
        _logger = logger;
        _bip39 = new BIP39();
    }

    public string Generate()
    {
        _logger.LogInformation("Generating new BIP39 mnemonic (256-bit entropy).");
        const int entropyBits = 256;

        var mnemonic = _bip39.GenerateMnemonic(entropyBits, BIP39Wordlist.English);
        _logger.LogDebug("New mnemonic generated.");
        return mnemonic;
    }

    public bool Validate(string mnemonic)
    {
        var isValid = _bip39.ValidateMnemonic(mnemonic, BIP39Wordlist.English);
        _logger.LogInformation("Validating mnemonic. Result: {IsValid}", isValid);
        return isValid;
    }

    public byte[] BackupEncryptionKey(string mnemonic, byte[] encryptionKey)
    {
        _logger.LogInformation("Starting encryption key backup process...");
        byte[] encryptedMainKey;

        try
        {
            string seedHex = _bip39.MnemonicToSeedHex(mnemonic, "");

            byte[] seed = Convert.FromHexString(seedHex);
            var recoveryEncryptionKey = new byte[32];
            Buffer.BlockCopy(seed, 0, recoveryEncryptionKey, 0, 32);

            encryptedMainKey = _cryptoService.EncryptBytes(encryptionKey, recoveryEncryptionKey);

            _logger.LogInformation("Encryption key successfully backed up using mnemonic.");

            Array.Clear(seed, 0, seed.Length);
            Array.Clear(encryptionKey, 0, encryptionKey.Length);
            Array.Clear(recoveryEncryptionKey, 0, recoveryEncryptionKey.Length);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "CRITICAL ERROR during encryption key backup! Mnemonic or crypto service failed.");
            throw;
        }

        return encryptedMainKey;
    }
}