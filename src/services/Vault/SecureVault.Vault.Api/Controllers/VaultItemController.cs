using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Contracts.DTOs.VaultItemDto;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Queries;
using SecureVault.Vault.Domain.Enums;
using System.Net;
using System.Security.Claims;

namespace SecureVault.Vault.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class VaultItemController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<VaultItemController> _logger;

    public VaultItemController(IMediator mediator, ILogger<VaultItemController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserVaultItemsAsync()
    {
        try
        {
            var userId = CurrentUserId;
            _logger.LogInformation("Getting all vault items for User: {UserId}", userId);
            var result = await _mediator.Send(new GetAllUserVaultItemsByUserIdQuery(userId));

            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt in GetUserVaultItems: {ErrorMessage}", ex.Message);
            return Unauthorized(new Error("Unauthorized", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetUserVaultItems for User: {UserId}", CurrentUserId);
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }

    [HttpGet("vault-items/type/{itemType}")]
    public async Task<IActionResult> GetUserVaultItemsByVaultTypeAsync(ItemType itemType)
    {
        try
        {
            var userId = CurrentUserId;
            _logger.LogInformation("Getting vault items for User: {UserId}, Type: {ItemType}", userId, itemType);
            var result = await _mediator.Send(new GetAllUserVaultItemsByVaultTypeQuery(itemType, userId));

            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt in GetUserVaultItemsByVaultType: {ErrorMessage}", ex.Message);
            return Unauthorized(new Error("Unauthorized", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetUserVaultItemsByVaultType for User: {UserId}", CurrentUserId);
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }

    [HttpGet("sync")]
    public async Task<IActionResult> GetUserVaultItemsByLastSyncTimeAsync([FromQuery] DateTimeOffset lastUpdateTime)
    {
        try
        {
            var userId = CurrentUserId;
            var deviceId = UniqueDeviceId;
            _logger.LogInformation("Sync request for User: {UserId}, Device: {DeviceId}, Since: {LastUpdateTime}", userId, deviceId, lastUpdateTime);
            var result = await _mediator.Send(new GetAllUserVaultItemsByLastSyncTimeQuery(userId, lastUpdateTime, deviceId));

            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt in GetUserVaultItemsByLastSyncTime: {ErrorMessage}", ex.Message);
            return Unauthorized(new Error("Unauthorized", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bad request in GetUserVaultItemsByLastSyncTime: {ErrorMessage}", ex.Message);
            return BadRequest(new Error("BadRequest", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetUserVaultItemsByLastSyncTime for User: {UserId}", CurrentUserId);
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVaultItemDto createVaultItemDto)
    {
        try
        {
            var userId = CurrentUserId;
            var deviceId = UniqueDeviceId;
            var command = new CreateVaultItemCommand(
                createVaultItemDto.Id, userId, createVaultItemDto.ItemType,
                createVaultItemDto.EncryptedData, createVaultItemDto.CreatedAt, deviceId
            );

            _logger.LogInformation("Creating vault item {ItemId} for User: {UserId}", command.Id, userId);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return StatusCode((int)HttpStatusCode.Created);

            _logger.LogWarning("Failed to create vault item {ItemId} for User {UserId}: {ErrorCode}", command.Id, userId, result.Error.Code);
            return BadRequest(result.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt in Create (VaultItem): {ErrorMessage}", ex.Message);
            return Unauthorized(new Error("Unauthorized", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bad request in GetUserVaultItemsByLastSyncTime: {ErrorMessage}", ex.Message);
            return BadRequest(new Error("BadRequest", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Create (VaultItem) for User: {UserId}", CurrentUserId);
            return StatusCode(500, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateVaultItemDto request)
    {
        try
        {
            var userId = CurrentUserId;
            var deviceId = UniqueDeviceId;
            var command = new UpdateVaultItemCommand(request.Id, userId, request.EncryptedData, request.UpdatedAt, deviceId);

            _logger.LogInformation("Updating vault item {ItemId} for User: {UserId}", command.Id, userId);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok();

            _logger.LogWarning("Failed to update vault item {ItemId} for User {UserId}: {ErrorCode}", command.Id, userId, result.Error.Code);
            return BadRequest(result.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt in Update (VaultItem): {ErrorMessage}", ex.Message);
            return Unauthorized(new Error("Unauthorized", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bad request in GetUserVaultItemsByLastSyncTime: {ErrorMessage}", ex.Message);
            return BadRequest(new Error("BadRequest", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Update (VaultItem) for User: {UserId}", CurrentUserId);
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = CurrentUserId;
            var deviceId = UniqueDeviceId;
            var command = new DeleteVaultItemCommand(id, userId, deviceId);

            _logger.LogInformation("Deleting vault item {ItemId} for User: {UserId}", command.Id, userId);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return NoContent();

            _logger.LogWarning("Failed to delete vault item {ItemId} for User {UserId}: {ErrorCode}", command.Id, userId, result.Error.Code);
            return BadRequest(result.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt in Delete (VaultItem): {ErrorMessage}", ex.Message);
            return Unauthorized(new Error("Unauthorized", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bad request in GetUserVaultItemsByLastSyncTime: {ErrorMessage}", ex.Message);
            return BadRequest(new Error("BadRequest", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Delete (VaultItem) for User: {UserId}", CurrentUserId);
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }

    [HttpPost("batch")]
    public async Task<IActionResult> CreateMany([FromBody] IEnumerable<CreateVaultItemDto> createVaultItemDtos)
    {
        try
        {
            var userId = CurrentUserId;
            var deviceId = UniqueDeviceId;
            var items = createVaultItemDtos.Select(dto => new CreateVaultItemCommand(
                dto.Id, userId, dto.ItemType, dto.EncryptedData, dto.CreatedAt, deviceId
            )).ToList();

            _logger.LogInformation("Creating {ItemCount} batch vault items for User: {UserId}", items.Count, userId);
            var result = await _mediator.Send(new CreateVaultItemListCommand(items));

            if (result.IsSuccess)
                return StatusCode((int)HttpStatusCode.Created);

            _logger.LogWarning("Failed to create batch vault items for User {UserId}: {ErrorCode}", userId, result.Error.Code);
            return BadRequest(result.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt in CreateMany (VaultItem): {ErrorMessage}", ex.Message);
            return Unauthorized(new Error("Unauthorized", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bad request in GetUserVaultItemsByLastSyncTime: {ErrorMessage}", ex.Message);
            return BadRequest(new Error("BadRequest", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in CreateMany (VaultItem) for User: {UserId}", CurrentUserId);
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }

    [HttpPut("batch")]
    public async Task<IActionResult> UpdateMany([FromBody] IEnumerable<UpdateVaultItemDto> updateVaultItemDtos)
    {
        try
        {
            var userId = CurrentUserId;
            var deviceId = UniqueDeviceId;
            var items = updateVaultItemDtos.Select(item => new UpdateVaultItemCommand(
                item.Id, userId, item.EncryptedData, item.UpdatedAt, deviceId
            )).ToList();

            _logger.LogInformation("Updating {ItemCount} batch vault items for User: {UserId}", items.Count, userId);
            var result = await _mediator.Send(new UpdateVaultItemListCommand(items));

            if (result.IsSuccess)
                return Ok();

            _logger.LogWarning("Failed to update batch vault items for User {UserId}: {ErrorCode}", userId, result.Error.Code);
            return BadRequest(result.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt in UpdateMany (VaultItem): {ErrorMessage}", ex.Message);
            return Unauthorized(new Error("Unauthorized", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bad request in GetUserVaultItemsByLastSyncTime: {ErrorMessage}", ex.Message);
            return BadRequest(new Error("BadRequest", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in UpdateMany (VaultItem) for User: {UserId}", CurrentUserId);
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }

    [HttpDelete("batch")]
    public async Task<IActionResult> DeleteMany([FromBody] IEnumerable<Guid> ids)
    {
        try
        {
            var userId = CurrentUserId;
            var deviceId = UniqueDeviceId;
            var items = new DeleteVaultItemListCommand(ids, userId, deviceId);

            _logger.LogInformation("Deleting {ItemCount} batch vault items for User: {UserId}", ids.Count(), userId);
            var result = await _mediator.Send(items);

            if (result.IsSuccess)
                return NoContent();

            _logger.LogWarning("Failed to delete batch vault items for User {UserId}: {ErrorCode}", userId, result.Error.Code);
            return BadRequest(result.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt in DeleteMany (VaultItem): {ErrorMessage}", ex.Message);
            return Unauthorized(new Error("Unauthorized", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bad request in GetUserVaultItemsByLastSyncTime: {ErrorMessage}", ex.Message);
            return BadRequest(new Error("BadRequest", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in DeleteMany (VaultItem) for User: {UserId}", CurrentUserId);
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }

    private string UniqueDeviceId
    {
        get
        {
            var deviceId = Request.Headers["X-Device-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(deviceId))
                throw new InvalidOperationException("Missing required X-Device-Id header.");
            return deviceId;
        }
    }
    private Guid CurrentUserId
    {
        get
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("User ID not found in token claims.");
            return Guid.Parse(userIdClaim);
        }
    }
}
