using MediatR;
using SecureVault.App.Domain.Enums;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Commands
{
    public class CreateTwoFactorAuthCodeCommand : IRequest<Result>
    {
        public string Issuer { get; set; }
        public string AccountName { get; set; }
        public string SecretKey { get; set; }
        public OtpType Type { get; set; }
        public int Digits { get; set; }
        public int Period { get; set; }
        public long Counter { get; set; }
        public OtpAlgorithm Algorithm { get; set; }
    }
}
