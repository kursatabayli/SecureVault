using MediatR;
using SecureVault.App.Application.Features.CQRS.Passwords.Results;

namespace SecureVault.App.Application.Features.CQRS.Passwords.Queries
{
    public class GetAllPasswordsQuery : IRequest<List<PasswordResult>>
    {
    }
}
