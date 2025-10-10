using MediatR;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Auth.Commands
{
    public class LoginWithQrCodeCommand : IRequest<Result>
    {
        public string Email { get; set; }
        public byte[] PrivateKey { get; set; }
        public byte[] EncryptionKey { get; set; }
        public bool RememberMe { get; set; }
    }
}
