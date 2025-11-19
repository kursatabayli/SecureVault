using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Features.CQRS.Sessions.Queries;
using SecureVault.App.Application.Features.CQRS.Sessions.Results;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Sessions.Handlers;

public class GetUserSessionsQueryHandler : IRequestHandler<GetUserSessionsQuery, Result<List<UserSessionsResult>>>
{
    private readonly IUserSessionService _userSessionService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserSessionsQueryHandler> _logger;

    public GetUserSessionsQueryHandler(IUserSessionService userSessionService, IMapper mapper, ILogger<GetUserSessionsQueryHandler> logger)
    {
        _userSessionService = userSessionService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<List<UserSessionsResult>>> Handle(GetUserSessionsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to fetch user sessions from API...");

        var apiResult = await _userSessionService.GetUserSessionsAsync(cancellationToken);

        if (apiResult.IsFailure)
        {
            _logger.LogWarning("Failed to fetch user sessions from API: {ErrorCode} - {ErrorMessage}",
                apiResult.Error.Code, apiResult.Error.Message);
            return apiResult.Error;
        }

        try
        {
            var sessions = apiResult.Value;
            _logger.LogInformation("Successfully fetched {Count} sessions. Mapping results...", sessions?.Count ?? 0);

            var models = _mapper.Map<List<UserSessionsResult>>(sessions);

            _logger.LogInformation("Successfully mapped {Count} session results.", models.Count);
            return Result<List<UserSessionsResult>>.Success(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A critical error occurred during mapping user session results.");
            return Result<List<UserSessionsResult>>.Failure(new Error("Client.MappingError", "An internal client mapping error occurred."));
        }
    }
}
