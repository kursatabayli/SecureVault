using AutoMapper;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Commands;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Results;
using SecureVault.App.Models.TwoFactorAuthCodeModels;

namespace SecureVault.App.Mappings
{
    public class TwoFactorAuthCodeMappingProfile : Profile
    {
        public TwoFactorAuthCodeMappingProfile()
        {
            CreateMap<TwoFactorAuthCodeResult, TwoFactorAuthCodeModel>();
            CreateMap<TwoFactorAuthCodeModel, CreateTwoFactorAuthCodeCommand>();
        }
    }
}
