using AutoMapper;
using SecureVault.App.Application.Features.CQRS.Sessions.Results;
using SecureVault.App.Models.SessionModels;

namespace SecureVault.App.Mappings
{
    public class SessionMappingProfile : Profile
    {
        public SessionMappingProfile()
        {
            CreateMap<UserSessionsResult, UserSessionsModel>();
            CreateMap<DeviceDetailResult, DeviceDetailModel>();
        }
    }
}
