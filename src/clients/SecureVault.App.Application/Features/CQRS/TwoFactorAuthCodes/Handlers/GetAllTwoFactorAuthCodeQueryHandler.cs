using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Queries;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Results;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Handlers;

internal class GetAllTwoFactorAuthCodeQueryHandler : IRequestHandler<GetAllTwoFactorAuthCodeQuery, Result<List<TwoFactorAuthCodeResult>>>
{
    private readonly ITwoFactorAuthCodeRepository _twoFactorAuthCodeRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllTwoFactorAuthCodeQueryHandler> _logger;

    public GetAllTwoFactorAuthCodeQueryHandler(ITwoFactorAuthCodeRepository twoFactorAuthCodeRepository, IMapper mapper, ILogger<GetAllTwoFactorAuthCodeQueryHandler> logger)
    {
        _twoFactorAuthCodeRepository = twoFactorAuthCodeRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<List<TwoFactorAuthCodeResult>>> Handle(GetAllTwoFactorAuthCodeQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Attempting to retrieve all 2FA codes from local Realm DB...");

            var twoFactorAuthCodeQuery = await _twoFactorAuthCodeRepository.GetAllAsync();
            var twoFactorAuthCodeEntities = twoFactorAuthCodeQuery.ToList();

            _logger.LogInformation("Successfully retrieved {Count} 2FA code entries.", twoFactorAuthCodeEntities.Count);

            var mappedResult = _mapper.Map<List<TwoFactorAuthCodeResult>>(twoFactorAuthCodeEntities);

            return Result<List<TwoFactorAuthCodeResult>>.Success(mappedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve 2FA codes from local database.");
            var error = new Error("LOCAL_DB_READ_FAILED", "Could not retrieve 2FA codes from your device.");
            return Result<List<TwoFactorAuthCodeResult>>.Failure(error);
        }
    }
}
