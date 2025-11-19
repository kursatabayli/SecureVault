using MediatR;
using SecureVault.App.Application.Features.CQRS.Passwords.Results;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Passwords.Queries;

public class GetAllPasswordsQuery : IRequest<Result<List<PasswordResult>>>
{
}
