using MediatR;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Results;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Queries;

public class GetAllTwoFactorAuthCodeQuery : IRequest<Result<List<TwoFactorAuthCodeResult>>>
{
}
