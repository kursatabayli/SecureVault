using AutoMapper;
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

        [HttpGet]
        public async Task<IActionResult> GetUserVaultItemsAsync()
        {
            var result = await _mediator.Send(new GetAllUserVaultItemsByUserIdQuery(CurrentUserId));
            if (result.IsSuccess)
                return Ok(result.Value);
            else
                return BadRequest(result.Error);
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

        [HttpGet("sync")]
        public async Task<IActionResult> GetUserVaultItemsByLastSyncTimeAsync([FromQuery] DateTimeOffset lastUpdateTime)
        {
            var result = await _mediator.Send(new GetAllUserVaultItemsByLastSyncTimeQuery(CurrentUserId, lastUpdateTime, UniqueDeviceId));
            if (result.IsSuccess)
                return Ok(result.Value);
            else
                return BadRequest(result.Error);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVaultItemDto createVaultItemDto)
        {
            var command = new CreateVaultItemCommand(
                createVaultItemDto.Id,
                CurrentUserId,
                createVaultItemDto.ItemType,
                createVaultItemDto.EncryptedData,
                createVaultItemDto.CreatedAt,
                UniqueDeviceId
            );
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateVaultItemDto request)
        {
            var command = new UpdateVaultItemCommand(request.Id, CurrentUserId, request.EncryptedData, request.UpdatedAt, UniqueDeviceId);
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(result);
            else
                return BadRequest(result.Error);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteVaultItemCommand(id, CurrentUserId, UniqueDeviceId);
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CreateMany([FromBody] IEnumerable<CreateVaultItemDto> createVaultItemDtos)
        {
            var items = createVaultItemDtos.Select(dto => new CreateVaultItemCommand(
                dto.Id,
                CurrentUserId,
                dto.ItemType,
                dto.EncryptedData,
                dto.CreatedAt,
                UniqueDeviceId
            ));

            var result = await _mediator.Send(new CreateVaultItemListCommand(items));

            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }

        [HttpPut("batch")]
        public async Task<IActionResult> UpdateMany([FromBody] IEnumerable<UpdateVaultItemDto> updateVaultItemDtos)
        {
            var items = updateVaultItemDtos.Select(item => new UpdateVaultItemCommand(
                item.Id,
                CurrentUserId,
                item.EncryptedData,
                item.UpdatedAt,
                UniqueDeviceId));

            var result = await _mediator.Send(new UpdateVaultItemListCommand(items));
            if (result.IsSuccess)
                return Ok(result);
            else
                return BadRequest(result.Error);
        }

        [HttpDelete("batch")]
        public async Task<IActionResult> DeleteMany(IEnumerable<Guid> ids)
        {
            var items = new DeleteVaultItemListCommand(ids, CurrentUserId, UniqueDeviceId);
            var result = await _mediator.Send(items);
            if (result.IsSuccess)
                return Ok();
            else
                return BadRequest(result.Error);
        }

        private string UniqueDeviceId => Request.Headers["X-Device-Id"].FirstOrDefault();
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
