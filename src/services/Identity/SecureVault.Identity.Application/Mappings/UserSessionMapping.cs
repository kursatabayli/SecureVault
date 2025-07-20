using AutoMapper;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Results;
using SecureVault.Identity.Domain.Entities;

namespace SecureVault.Identity.Application.Mappings
{
    public class UserSessionMapping : Profile
    {
        public UserSessionMapping()
        {
            CreateMap<DeviceDetail, DeviceDetailResult>();
            CreateMap<UserSession, UserSessionResult>();
        }
    }
}
