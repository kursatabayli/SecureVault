using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVault.Identity.Application.Contracts.DTOs.AuthDto;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using System.Security.Claims;

namespace SecureVault.Identity.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAuthService _authService;

        public AuthController(IMediator mediator, IAuthService authService)
        {
            _mediator = mediator;
            _authService = authService;
        }

        [HttpGet("challenge/{email}")]
        public async Task<IActionResult> RequestChallenge(string email)
        {
            var result = await _mediator.Send(new RequestLoginChallengeCommand(email));
            if (result.IsSuccess)
                return Ok(result.Value);
            else
                return BadRequest(result.Error);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCredentialsDto credentials, [FromQuery] bool rememberMe = false)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var uniqueDeviceId = Request.Headers["X-Device-Id"].FirstOrDefault();
            var deviceName = Request.Headers["X-Device-Name"].FirstOrDefault();
            var deviceModel = Request.Headers["X-Device-Model"].FirstOrDefault();
            var deviceManufacturer = Request.Headers["X-Device-Manufacturer"].FirstOrDefault();
            var operatingSystem = Request.Headers["X-Device-OS"].FirstOrDefault();

            var command = new LoginUserCommand(
                credentials.Email,
                credentials.Signature,
                rememberMe,
                uniqueDeviceId,
                deviceName,
                deviceModel,
                deviceManufacturer,
                operatingSystem,
                ipAddress
            );

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result.Value);
            else
                return BadRequest(result.Error);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var command = new RefreshTokenCommand(
                Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last(),
                Request.Headers["X-Refresh-Token"].FirstOrDefault(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["X-Device-Id"].FirstOrDefault(),
                Request.Headers["X-Device-Name"].FirstOrDefault()
            );
            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var command = new LogoutUserCommand(
                Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last(),
                Request.Headers["X-Refresh-Token"].FirstOrDefault()
            );
            var result = await _mediator.Send(command);
            return Ok();
        }
    }
}
