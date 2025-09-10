using AutoMapper;
using SecureVault.App.Application.Features.CQRS.Passwords.Commands;
using SecureVault.App.Application.Features.CQRS.Passwords.Results;
using SecureVault.App.Models.PassowordModels;

namespace SecureVault.App.Mappings
{
    public class PasswordMappingProfile : Profile
    {
        public PasswordMappingProfile()
        {
            CreateMap<PasswordResult, PasswordModel>();
            CreateMap<CreatePasswordModel, CreatePasswordCommand>();
        }
    }
}
