using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVault.Vault.Application.Contracts.DTOs.VaultItemDto;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Queries;
using SecureVault.Vault.Domain.Enums;
using System.Security.Claims;

namespace SecureVault.Vault.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VaultItemController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VaultItemController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("vault-items/type/{itemType}")]
        public async Task<IActionResult> GetUserVaultItemsByVaultTypeAsync(ItemType itemType)
        {
            var result = await _mediator.Send(new GetAllUserVaultItemsByVaultTypeQuery(itemType, CurrentUserId));
            if (result.IsSuccess)
                return Ok(result.Value);
            else
                return BadRequest(result.Error);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVaultItemCommand command)
        {
            var finalCommand = command with { UserId = CurrentUserId };
            var result = await _mediator.Send(finalCommand);
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVaultItemDto request)
        {
            var command = new UpdateVaultItemCommand(id, CurrentUserId, request.EncryptedData);
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(result);
            else
                return BadRequest(result.Error);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteVaultItemCommand(id, CurrentUserId);
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }

        private Guid CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı");
                }
                return Guid.Parse(userIdClaim);
            }
        }
    }
}
