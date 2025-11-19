using AutoMapper;
using SecureVault.App.Application.Features.CQRS.Auth.Commands;
using SecureVault.App.Models.LoginModels;

namespace SecureVault.App.Mappings;

public class LoginMappingProfile : Profile
{
    public LoginMappingProfile()
    {
        CreateMap<LoginModel, LoginCommand>();
        CreateMap<LoginQrCodeModel, LoginWithQrCodeCommand>();
    }
}
