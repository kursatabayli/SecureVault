using AutoMapper;
using MediatR;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Features.CQRS.Sessions.Queries;
using SecureVault.App.Application.Features.CQRS.Sessions.Results;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Sessions.Handlers
{
    public class GetUserSessionsQueryHandler : IRequestHandler<GetUserSessionsQuery, Result<List<UserSessionsResult>>>
    {
        private readonly IUserSessionService _userSessionService;
        private readonly IMapper _mapper;

        public GetUserSessionsQueryHandler(IUserSessionService userSessionService, IMapper mapper)
        {
            _userSessionService = userSessionService;
            _mapper = mapper;
        }

        public async Task<Result<List<UserSessionsResult>>> Handle(GetUserSessionsQuery request, CancellationToken cancellationToken)
        {
            var result = await _userSessionService.GetUserSessionsAsync(cancellationToken);

            if (result.IsFailure)
                return result.Error;

            var models = _mapper.Map<List<UserSessionsResult>>(result.Value);

            return models;
        }
    }
}
