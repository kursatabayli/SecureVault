using AutoMapper;
using MediatR;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Features.CQRS.Auth.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Auth.Handlers
{
    internal class LoginWithQrCodeHandler : IRequestHandler<LoginWithQrCodeCommand, Result>
    {
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;
        public LoginWithQrCodeHandler(IAuthService authService, IMapper mapper)
        {
            _authService = authService;
            _mapper = mapper;
        }

        public async Task<Result> Handle(LoginWithQrCodeCommand request, CancellationToken cancellationToken)
        {
            var loginDto = _mapper.Map<LoginQrCodeDto>(request);

            var result = await _authService.LoginWithQrCodeAsync(loginDto, cancellationToken);

            return result;
        }
    }
}
