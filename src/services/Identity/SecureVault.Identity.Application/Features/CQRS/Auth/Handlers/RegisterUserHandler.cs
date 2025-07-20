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
    public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<ReturnMessages> _returnMessages;
        private readonly ILogger<RegisterUserHandler> _logger;

        public RegisterUserHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, IStringLocalizer<ReturnMessages> returnMessages, ILogger<RegisterUserHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _returnMessages = returnMessages;
            _logger = logger;
        }

        public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser is not null)
                    return Result.Failure(new Error(ErrorCodes.Auth.EmailInUse, _returnMessages[ErrorCodes.Auth.EmailInUse]));

                var newUser = User.Create(request.Email, request.PublicKey, request.Salt, request.UserInfo);

                await _userRepository.AddAsync(newUser);
                await _unitOfWork.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı kaydı sırasında beklenmedik bir hata oluştu. Email: {Email}", request.Email);
                return Result.Failure(new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]));
            }
        }
    }
}
