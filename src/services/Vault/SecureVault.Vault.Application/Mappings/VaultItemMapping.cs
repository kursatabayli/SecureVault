using AutoMapper;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Results;
using SecureVault.Vault.Domain.Entities;

namespace SecureVault.Vault.Application.Mappings
{
    public class VaultItemMapping : Profile
    {
        public VaultItemMapping() 
        {
            CreateMap<VaultItem, VaultItemResult>();
        }
    }
}
