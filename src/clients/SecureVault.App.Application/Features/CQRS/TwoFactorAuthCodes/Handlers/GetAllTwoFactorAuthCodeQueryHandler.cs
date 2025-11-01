using AutoMapper;
using MediatR;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Queries;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Results;

namespace SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Handlers
{
    internal class GetAllTwoFactorAuthCodeQueryHandler : IRequestHandler<GetAllTwoFactorAuthCodeQuery, List<TwoFactorAuthCodeResult>>
    {
        private readonly ITwoFactorAuthCodeRepository _twoFactorAuthCodeRepository;
        private readonly IMapper _mapper;

        public GetAllTwoFactorAuthCodeQueryHandler(ITwoFactorAuthCodeRepository twoFactorAuthCodeRepository, IMapper mapper)
        {
            _twoFactorAuthCodeRepository = twoFactorAuthCodeRepository;
            _mapper = mapper;
        }

        public async Task<List<TwoFactorAuthCodeResult>> Handle(GetAllTwoFactorAuthCodeQuery request, CancellationToken cancellationToken)
        {
            var twoFactorAuthCodeQuery = await _twoFactorAuthCodeRepository.GetAllAsync();
            var twoFactorAuthCodeEntities = twoFactorAuthCodeQuery.ToList();
            return _mapper.Map<List<TwoFactorAuthCodeResult>>(twoFactorAuthCodeEntities);
        }
    }
}
