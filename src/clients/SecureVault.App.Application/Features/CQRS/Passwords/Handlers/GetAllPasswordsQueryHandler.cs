using AutoMapper;
using MediatR;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Application.Features.CQRS.Passwords.Queries;
using SecureVault.App.Application.Features.CQRS.Passwords.Results;

namespace SecureVault.App.Application.Features.CQRS.Passwords.Handlers
{
    public class GetAllPasswordsQueryHandler : IRequestHandler<GetAllPasswordsQuery, List<PasswordResult>>
    {
        private readonly IPasswordRepository _passwordRepository;
        private readonly IMapper _mapper;

        public GetAllPasswordsQueryHandler(IPasswordRepository passwordRepository, IMapper mapper)
        {
            _passwordRepository = passwordRepository;
            _mapper = mapper;
        }

        public async Task<List<PasswordResult>> Handle(GetAllPasswordsQuery request, CancellationToken cancellationToken)
        {
            var passwordEntities = await _passwordRepository.GetAllAsync();
            return _mapper.Map<List<PasswordResult>>(passwordEntities);
        }
    }
}
