using MediatR;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.DTOs.Register;
using SecureVault.App.Application.Contracts.DTOs.User;
using SecureVault.App.Application.Features.CQRS.Register.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Register.Handlers
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result>
    {
        private readonly IHashService _hashService;
        private readonly IBouncyCastleCryptoService _bouncyCastleCryptoService;
        private readonly IRegisterService _registerService;
        private readonly IBip39RecoveryKeyService _bip39RecoveryKeyService;
        public RegisterUserCommandHandler(
            IHashService hashService,
            IBouncyCastleCryptoService bouncyCastleCryptoService,
            IRegisterService registerService,
            IBip39RecoveryKeyService bip39RecoveryKeyService)
        {
            _hashService = hashService;
            _bouncyCastleCryptoService = bouncyCastleCryptoService;
            _registerService = registerService;
            _bip39RecoveryKeyService = bip39RecoveryKeyService;
        }

        public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var (Salt, PublicKey) = _bouncyCastleCryptoService.GenerateValidKeyPair(request.Password);
            var publicKey = PublicKey;
            var salt = Salt;

            var encryptedRecoveryData = await CreateEncryptionKeyBackup(request.Password, salt, request.MnemonicRecoveryKey);
            var userInfoDto = new UserInfoDto
            {
                Name = request.Name,
                Surname = request.Surname,
                PhoneNumber = request.PhoneNumber
            };

            var registerUserDto = new RegisterUserDto
            {
                Email = request.Email,
                PublicKey = publicKey,
                Salt = salt,
                UserInfo = userInfoDto,
                RecoveryData = encryptedRecoveryData
            };

            var registrationResult = await _registerService.RegisterAsync(registerUserDto, cancellationToken);
            if (registrationResult.IsFailure)
            {
                return registrationResult;
            }
            return Result.Success();
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
