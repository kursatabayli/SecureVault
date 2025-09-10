using AutoMapper;
using MediatR;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Features.CQRS.Auth.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Auth.Handlers
{
    internal class LoginHandler : IRequestHandler<LoginCommand, Result>
    {
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;
        public LoginHandler(IAuthService authService, IMapper mapper)
        {
            _authService = authService;
            _mapper = mapper;
        }

        public async Task<Result> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var loginDto = _mapper.Map<LoginDto>(request);

            var result = await _authService.LoginAsync(loginDto, cancellationToken);

            return result;
        }
    }
}
