using MediatR;
using SecureVault.Identity.Application.Contracts.DTOs.AuthDto;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.Auth.Commands
{
    public record RequestLoginChallengeCommand(string Email) : IRequest<Result<ChallengeDto>>;
}
