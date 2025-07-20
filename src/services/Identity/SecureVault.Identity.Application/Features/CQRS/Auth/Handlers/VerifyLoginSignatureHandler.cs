using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Application.Messages;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.Auth.Handlers
{
    public class VerifyLoginSignatureHandler : IRequestHandler<VerifyLoginSignatureCommand, Result<User>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cacheService;
        private readonly IEcdsaVerificationService _schnorrService;
        private readonly IStringLocalizer<ReturnMessages> _returnMessages;
        private readonly ILogger<VerifyLoginSignatureHandler> _logger;

        private const string LoginChallengeCacheKeyPrefix = "login_challenge:";
        public VerifyLoginSignatureHandler(IUserRepository userRepository, ICacheService cacheService, IEcdsaVerificationService schnorrService, IStringLocalizer<ReturnMessages> returnMessages, ILogger<VerifyLoginSignatureHandler> logger)
        {
            _userRepository = userRepository;
            _cacheService = cacheService;
            _schnorrService = schnorrService;
            _returnMessages = returnMessages;
            _logger = logger;
        }

        public async Task<Result<User>> Handle(VerifyLoginSignatureCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user is null)
                    return new Error(ErrorCodes.Auth.LoginFailed, _returnMessages[ErrorCodes.Auth.LoginFailed]);

                var cacheKey = $"{LoginChallengeCacheKeyPrefix}{user.Id}";
                string? challenge = await _cacheService.GetAsync<string>(cacheKey);

                if (string.IsNullOrEmpty(challenge))
                    return new Error(ErrorCodes.Auth.ChallengeExpired, _returnMessages[ErrorCodes.Auth.ChallengeExpired]);

                bool isSignatureValid = _schnorrService.VerifySignature(challenge, request.Signature, user.PublicKey);

                if (!isSignatureValid)
                    return new Error(ErrorCodes.Auth.LoginFailed, _returnMessages[ErrorCodes.Auth.LoginFailed]);

                await _cacheService.RemoveAsync(cacheKey);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login imza doğrulaması sırasında beklenmedik bir hata oluştu. Email: {Email}", request.Email);
                return new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]);
            }
        }
    }
}

