using AutoMapper;
using SecureVault.App.Application.Contracts.DTOs.Passwords;
using SecureVault.App.Application.Features.CQRS.Passwords.Results;
using SecureVault.App.Domain.Entities;

namespace SecureVault.App.Application.Mappings;

public class PasswordMappingProfile : Profile
{
    public PasswordMappingProfile()
    {
        CreateMap<PasswordEntity, PasswordResult>();
        CreateMap<PasswordEntity, PasswordDto>();
    }
}
