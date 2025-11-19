using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Contracts.DTOs.Register;
using SecureVault.App.Application.Contracts.DTOs.User;
using SecureVault.App.Application.Features.CQRS.Register.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Register.Handlers;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result>
{
    private readonly IHashService _hashService;
    private readonly IBouncyCastleCryptoService _bouncyCastleCryptoService;
    private readonly IRegisterService _registerService;
    private readonly IBip39RecoveryKeyService _bip39RecoveryKeyService;
    private readonly ILogger<RegisterUserCommandHandler> _logger;
    public RegisterUserCommandHandler(
        IHashService hashService,
        IBouncyCastleCryptoService bouncyCastleCryptoService,
        IRegisterService registerService,
        IBip39RecoveryKeyService bip39RecoveryKeyService,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _hashService = hashService;
        _bouncyCastleCryptoService = bouncyCastleCryptoService;
        _registerService = registerService;
        _bip39RecoveryKeyService = bip39RecoveryKeyService;
        _logger = logger;
    }

    public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // YENİ EKLENDİ (1): "Happy path" logu
        _logger.LogInformation("Registration process started for email: {Email}", request.Email);

        try
        {
            _logger.LogDebug("Starting client-side key generation and crypto in a background thread...");
            var (publicKey, salt, encryptedRecoveryData) = await Task.Run(() =>
            {
                var (Salt, PublicKey) = _bouncyCastleCryptoService.GenerateValidKeyPair(request.Password);
                var encryptedData = CreateEncryptionKeyBackup(request.Password, Salt, request.MnemonicRecoveryKey);

                return (PublicKey, Salt, encryptedData);
            }, cancellationToken);

            _logger.LogDebug("Client-side crypto complete.");

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

            _logger.LogInformation("Sending registration request to API for email: {Email}", request.Email);
            var registrationResult = await _registerService.RegisterAsync(registerUserDto, cancellationToken);

            if (registrationResult.IsFailure)
            {
                _logger.LogWarning("User registration failed (API business logic error) for {Email}: {ErrorCode} - {ErrorMessage}",
                    request.Email, registrationResult.Error.Code, registrationResult.Error.Message);
                return registrationResult;
            }

            _logger.LogInformation("User registration successful for email: {Email}", request.Email);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during the registration process for email: {Email}", request.Email);
            return Result.Failure(new Error("Client.Register.Unexpected", "An unexpected error occurred during registration."));
        }
    }

    private byte[] CreateEncryptionKeyBackup(string password, byte[] salt, string mnemonic)
    {
        _logger.LogDebug("Creating encryption key backup from mnemonic...");
        var masterSecret = _hashService.CreateMasterSecret(password, salt);
        var encryptionKey = _hashService.GetEncryptionKeyForData(masterSecret, salt);
        var encryptionKeyBackup = _bip39RecoveryKeyService.BackupEncryptionKey(mnemonic, encryptionKey);

        _logger.LogDebug("Encryption key backup created successfully.");
        return encryptionKeyBackup;
    }
}
