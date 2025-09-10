using MediatR;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Results;

namespace SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Queries
{
    public class GetAllTwoFactorAuthCodeQuery : IRequest<List<TwoFactorAuthCodeResult>>
    {
    }
}
