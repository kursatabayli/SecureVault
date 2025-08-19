using Microsoft.Extensions.Logging;
using SecureVault.App.Services.Models.RecoveryKeyModels;
using SecureVault.App.Services.Models.RegisterModels;
using SecureVault.App.Services.Service.Application.Contracts;
using SecureVault.App.Services.Service.Infrastructure.Contracts;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services
{
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly IRegisterService _registerService;
        private readonly IBouncyCastleCryptoService _cryptoService;
        private readonly IHashService _hashService;
        private readonly IBip39RecoveryKeyService _bip39RecoveryKeyService;
        private readonly ILogger<UserRegistrationService> _logger;

        public UserRegistrationService(
            IRegisterService registerService,
            IBouncyCastleCryptoService cryptoService,
            IHashService hashService,
            IBip39RecoveryKeyService bip39RecoveryKeyService,
            ILogger<UserRegistrationService> logger)
        {
            _registerService = registerService;
            _cryptoService = cryptoService;
            _hashService = hashService;
            _bip39RecoveryKeyService = bip39RecoveryKeyService;
            _logger = logger;
        }

        public async Task<Result> RegisterAndBackupAsync(RegisterUserModel registerUserModel, string password, RecoveryKeyModel recoveryKey)
        {
            try
            {
                var (salt, publicKey) = _cryptoService.GenerateValidKeyPair(password);
                registerUserModel.Salt = salt;
                registerUserModel.PublicKey = publicKey;

                registerUserModel.RecoveryData = await CreateEncryptionKeyBackup(password, salt, recoveryKey.Mnemonic);

                var registrationResult = await _registerService.RegisterAsync(registerUserModel);
                if (!registrationResult.IsSuccess)
                    return registrationResult;

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı kayıt sürecinde beklenmedik bir hata oluştu (E-posta: {Email}).", registerUserModel.Email);
                var error = new Error(ErrorCodes.UnexpectedError, "Kayıt işlemi sırasında beklenmedik bir hata oluştu.");
                return Result.Failure(error);
            }
        }

        private Task<byte[]> CreateEncryptionKeyBackup(string password, byte[] salt, string mnemonic)
        {
            var masterSecret = _hashService.CreateMasterSecret(password, salt);
            var encryptionKey = _hashService.GetEncryptionKeyForData(masterSecret, salt);
            var encryptionKeyBackup = _bip39RecoveryKeyService.BackupEncryptionKey(mnemonic, encryptionKey);

            return Task.FromResult(encryptionKeyBackup);

        }
    }
}
