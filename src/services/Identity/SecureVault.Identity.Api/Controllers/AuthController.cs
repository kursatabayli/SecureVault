using System.Net;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVault.Identity.Application.Contracts.DTOs.AuthDto;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("challenge/{email}")]
    public async Task<IActionResult> RequestChallenge(string email)
    {
        _logger.LogInformation("Requesting login challenge for email: {Email}", email);
        try
        {
            var result = await _mediator.Send(new RequestLoginChallengeCommand(email));
            if (result.IsSuccess)
                return Ok(result.Value);

            _logger.LogWarning("RequestChallenge failed for {Email}: {ErrorCode} - {ErrorMessage}", email, result.Error.Code, result.Error.Message);
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during RequestChallenge for email: {Email}", email);
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCredentialsDto credentials, [FromQuery] bool rememberMe = false)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation("Login attempt received for email: {Email} from IP: {IpAddress}", credentials.Email, ipAddress);

        try
        {
            var uniqueDeviceId = Request.Headers["X-Device-Id"].FirstOrDefault();
            var jkt = Request.Headers["X-DPoP-JKT"].FirstOrDefault();

            if (string.IsNullOrEmpty(uniqueDeviceId) || string.IsNullOrEmpty(jkt))
            {
                _logger.LogWarning("Login failed for {Email}: Missing required headers. DeviceId provided: {HasDeviceId}, JKT provided: {HasJkt}",
                    credentials.Email, !string.IsNullOrEmpty(uniqueDeviceId), !string.IsNullOrEmpty(jkt));
                return BadRequest(new Error("MissingHeaders", "Missing required X-Device-Id or X-DPoP-JKT headers."));
            }

            var deviceName = Request.Headers["X-Device-Name"].FirstOrDefault();
            var deviceModel = Request.Headers["X-Device-Model"].FirstOrDefault();
            var deviceManufacturer = Request.Headers["X-Device-Manufacturer"].FirstOrDefault();
            var operatingSystem = Request.Headers["X-Device-OS"].FirstOrDefault();

            var command = new LoginUserCommand(
                credentials.Email, credentials.Signature, rememberMe,
                uniqueDeviceId, deviceName, deviceModel, deviceManufacturer,
                operatingSystem, ipAddress, jkt
            );

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result.Value);

            _logger.LogWarning("Login failed for {Email}: {ErrorCode} - {ErrorMessage}", credentials.Email, result.Error.Code, result.Error.Message);
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for email: {Email}", credentials.Email);
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation("Token refresh request received from IP: {IpAddress}", ipAddress);

        try
        {
            var accessToken = Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
            var refreshToken = Request.Headers["X-Refresh-Token"].FirstOrDefault();
            var jkt = Request.Headers["X-DPoP-JKT"].FirstOrDefault();

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(jkt))
            {
                _logger.LogWarning("Token refresh failed: Missing tokens or DPoP JKT. AccessToken: {HasAccessToken}, RefreshToken: {HasRefreshToken}, JKT: {HasJkt}",
                   !string.IsNullOrEmpty(accessToken), !string.IsNullOrEmpty(refreshToken), !string.IsNullOrEmpty(jkt));
                return BadRequest(new Error("MissingTokens", "Access token, refresh token, or DPoP JKT is missing."));
            }

            var command = new RefreshTokenCommand(
                accessToken, refreshToken, ipAddress,
                Request.Headers["X-Device-Id"].FirstOrDefault(),
                Request.Headers["X-Device-Name"].FirstOrDefault(),
                jkt
            );

            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh from IP: {IpAddress}", ipAddress);
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("Logout request received.");

        try
        {
            var accessToken = Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
            var refreshToken = Request.Headers["X-Refresh-Token"].FirstOrDefault();
            var jkt = Request.Headers["X-DPoP-JKT"].FirstOrDefault();

            if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(jkt))
            {
                _logger.LogWarning("Logout failed: Missing refresh token or DPoP JKT. RefreshToken: {HasRefreshToken}, JKT: {HasJkt}",
                    !string.IsNullOrEmpty(refreshToken), !string.IsNullOrEmpty(jkt));
                return BadRequest(new Error("MissingTokens", "Refresh token or DPoP JKT is missing."));
            }

            var command = new LogoutUserCommand(accessToken, refreshToken, jkt);

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok();

            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during logout.");
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }
}
