using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVault.Identity.Application.Contracts.DTOs.AuthDto;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;

namespace SecureVault.Identity.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("challenge/{email}")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestChallenge(string email)
        {
            var result = await _mediator.Send(new RequestLoginChallengeCommand(email));
            if (result.IsSuccess)
                return Ok(result.Value);
            else
                return BadRequest(result.Error);
        }

        [HttpPost("login")]
        [AllowAnonymous]
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
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto requestDto)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var uniqueDeviceId = Request.Headers["X-Device-Id"].FirstOrDefault();
            var deviceName = Request.Headers["X-Device-Name"].FirstOrDefault();

            var command = new RefreshTokenCommand(
                requestDto.RefreshToken,
                requestDto.AccessToken,
                ipAddress,
                uniqueDeviceId,
                deviceName
            );

            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(result.Value);
            else
                return BadRequest(result.Error);
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto requestDto)
        {
            var command = new LogoutUserCommand(requestDto.RefreshToken);
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(result);
            else
                return BadRequest(result.Error);
        }
    }
}
