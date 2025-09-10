using AutoMapper;
using SecureVault.App.Application.Contracts.DTOs.TwoFactorAuthCodes;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Results;
using SecureVault.App.Domain.Entities;

namespace SecureVault.App.Application.Mappings
{
    public class TwoFactorAuthCodeMappingProfile : Profile
    {
        public TwoFactorAuthCodeMappingProfile()
        {
            CreateMap<TwoFactorAuthCodeEntity, TwoFactorAuthCodeResult>();
            CreateMap<TwoFactorAuthCodeEntity, TwoFactorAuthCodeDto>();
        }
    }
}
