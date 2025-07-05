using MediatR;
using Microsoft.AspNetCore.Mvc;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Queries;
using SecureVault.Vault.Domain.Enums;
using System.Security.Claims;

namespace SecureVault.Vault.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VaultItemController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VaultItemController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{itemType}")]
        public async Task<IActionResult> GetAllUserVaultItemsByVaultTypeAsync(ItemType itemType)
        {
            Guid randomGuid = Guid.Parse("1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d");
            var result = await _mediator.Send(new GetAllUserVaultItemsByVaultTypeQuery(itemType, randomGuid));
            if (result.IsSuccess)
                return Ok(result.Value);
            else
                return BadRequest(result.Error);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVaultItemCommand command)
        {
            var finalCommand = command with { UserId = Guid.Parse("1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d") };
            var result = await _mediator.Send(finalCommand);
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateVaultItemCommand command)
        {
            var finalCommand = command with { UserId = Guid.Parse("1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d") };
            var result = await _mediator.Send(finalCommand);
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteVaultItemCommand(id, Guid.Parse("1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d"));
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
