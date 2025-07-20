using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Identity.Application.Contracts.DTOs.AuthDto;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Application.Messages;
using SecureVault.Shared.Result;
using System.Security.Cryptography;

namespace SecureVault.Identity.Application.Features.CQRS.Auth.Handlers
{
    public class RequestLoginChallengeHandler : IRequestHandler<RequestLoginChallengeCommand, Result<ChallengeDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cacheService;
        private readonly IStringLocalizer<ReturnMessages> _returnMessages;
        private readonly ILogger<RequestLoginChallengeHandler> _logger;

        private const string LoginChallengeCacheKeyPrefix = "login_challenge:";
        public RequestLoginChallengeHandler(IUserRepository userRepository, ICacheService cacheService, IStringLocalizer<ReturnMessages> returnMessages, ILogger<RequestLoginChallengeHandler> logger)
        {
            _userRepository = userRepository;
            _cacheService = cacheService;
            _returnMessages = returnMessages;
            _logger = logger;
        }

        public async Task<Result<ChallengeDto>> Handle(RequestLoginChallengeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user is null)
                    return new Error(ErrorCodes.Auth.UserNotFound, _returnMessages[ErrorCodes.Auth.UserNotFound]);

                var challenge = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                var loginChallengeDto = new ChallengeDto { Challenge = challenge, Salt = user.Salt };
                var cacheKey = $"{LoginChallengeCacheKeyPrefix}{user.Id}";
                await _cacheService.SetAsync(cacheKey, challenge, TimeSpan.FromSeconds(60));

                return loginChallengeDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login challenge isteği sırasında beklenmedik bir hata oluştu. Email: {Email}", request.Email);
                return new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]);
            }

        }
    }
}
