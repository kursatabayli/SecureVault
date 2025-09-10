using AutoMapper;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Features.CQRS.Auth.Commands;

namespace SecureVault.App.Application.Mappings
{
    public class LoginMappingProfile : Profile
    {
        public LoginMappingProfile()
        {
            CreateMap<LoginCommand, LoginDto>();
            CreateMap<LoginWithQrCodeCommand, LoginQrCodeDto>();
            CreateMap<LoginQrCodeDto, LoginWithQrCodeCommand>();
        }
    }
}
