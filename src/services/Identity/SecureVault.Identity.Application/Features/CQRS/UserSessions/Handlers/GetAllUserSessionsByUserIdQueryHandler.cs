using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Queries;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Results;
using SecureVault.Identity.Application.Messages;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.UserSessions.Handlers;

public class GetAllUserSessionsByUserIdQueryHandler : IRequestHandler<GetAllUserSessionsByUserIdQuery, Result<IReadOnlyCollection<UserSessionResult>>>
{
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllUserSessionsByUserIdQueryHandler> _logger;
    private readonly IStringLocalizer<ReturnMessages> _returnMessages;

    public GetAllUserSessionsByUserIdQueryHandler(IUserSessionRepository userSessionRepository, IMapper mapper, ILogger<GetAllUserSessionsByUserIdQueryHandler> logger, IStringLocalizer<ReturnMessages> returnMessages)
    {
        _userSessionRepository = userSessionRepository;
        _mapper = mapper;
        _logger = logger;
        _returnMessages = returnMessages;
    }

    public async Task<Result<IReadOnlyCollection<UserSessionResult>>> Handle(GetAllUserSessionsByUserIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userSessions = await _userSessionRepository.GetAllSessionsByUserIdAsync(request.UserId);
            var userSessionResults = _mapper.Map<IReadOnlyCollection<UserSessionResult>>(userSessions);

            if (userSessionResults == null || !userSessionResults.Any())
            {
                _logger.LogInformation("No user sessions found for User: {UserId}", request.UserId);
            }
            else
            {
                _logger.LogInformation("Successfully retrieved {SessionCount} user sessions for User: {UserId}", userSessionResults.Count, request.UserId);
            }

            return Result<IReadOnlyCollection<UserSessionResult>>.Success(userSessionResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving user sessions. UserId: {UserId}", request.UserId);
            return new Error(ErrorCodes.InternalServerError, _returnMessages[ErrorCodes.InternalServerError]);
        }
    }
}
