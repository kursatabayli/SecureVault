using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Domain.Entities;

namespace SecureVault.Identity.Application.Services
{
    public interface IUserSessionService
    {
        Task ManageSessionAsync(User user, LoginUserCommand request, string newTokenIdentifier, DateTime newRefreshTokenExpiration);
    }
}
