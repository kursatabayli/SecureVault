using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Application.Features.CQRS.Passwords.Queries;
using SecureVault.App.Application.Features.CQRS.Passwords.Results;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Passwords.Handlers;

public class GetAllPasswordsQueryHandler : IRequestHandler<GetAllPasswordsQuery, Result<List<PasswordResult>>>
{
    private readonly IPasswordRepository _passwordRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllPasswordsQueryHandler> _logger;

    public GetAllPasswordsQueryHandler(IPasswordRepository passwordRepository, IMapper mapper, ILogger<GetAllPasswordsQueryHandler> logger)
    {
        _passwordRepository = passwordRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<List<PasswordResult>>> Handle(GetAllPasswordsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Attempting to retrieve all passwords from local Realm DB...");

            var passwordQuery = await _passwordRepository.GetAllAsync();
            var passwordEntities = passwordQuery.ToList();

            _logger.LogInformation("Successfully retrieved {Count} password entries.", passwordEntities.Count);

            var mappedResult = _mapper.Map<List<PasswordResult>>(passwordEntities);

            return mappedResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve passwords from local database.");
            var error = new Error("LOCAL_DB_READ_FAILED", "Could not retrieve passwords from your device.");
            return Result<List<PasswordResult>>.Failure(error);
        }
    }
}
