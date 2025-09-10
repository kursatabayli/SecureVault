using AutoMapper;
using SecureVault.App.Application.Contracts.DTOs.Session;
using SecureVault.App.Application.Features.CQRS.Sessions.Results;

namespace SecureVault.App.Application.Mappings
{
    public class SessionMappingProfile : Profile
    {
        public SessionMappingProfile()
        {
            CreateMap<UserSessionsDto, UserSessionsResult>();
            CreateMap<DeviceDetailDto, DeviceDetailResult>();
        }
    }
}
