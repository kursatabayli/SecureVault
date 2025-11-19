using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Commands;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Queries;
using SecureVault.Shared.Result;
using System.Net;
using System.Security.Claims;

namespace SecureVault.Identity.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserSessionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserSessionController> _logger;

    public UserSessionController(IMediator mediator, ILogger<UserSessionController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUserSessionsByUserId()
    {
        try
        {
            var userId = CurrentUserId;
            _logger.LogInformation("Attempting to get all user sessions for UserId: {UserId}", userId);

            var result = await _mediator.Send(new GetAllUserSessionsByUserIdQuery(userId));

            if (result.IsSuccess)
                return Ok(result.Value);

            _logger.LogWarning("Failed to get user sessions for UserId: {UserId}. Error: {ErrorCode}", userId, result.Error.Code);
            return BadRequest(result.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt in GetAllUserSessions: {ErrorMessage}", ex.Message);
            return Unauthorized(new Error("Unauthorized", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting user sessions.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId)
    {
        try
        {
            var userId = CurrentUserId;
            _logger.LogInformation("User {UserId} attempting to revoke session: {SessionId}", userId, sessionId);

            var command = new RevokeSessionCommand(sessionId, userId);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Session revoked successfully: {SessionId} by User {UserId}", sessionId, userId);
                return NoContent();
            }

            _logger.LogWarning("Failed to revoke session {SessionId} for User {UserId}. Error: {ErrorCode}", sessionId, userId, result.Error.Code);
            return BadRequest(result.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt in RevokeSession: {ErrorMessage}", ex.Message);
            return Unauthorized(new Error("Unauthorized", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while revoking session: {SessionId}", sessionId);
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
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
