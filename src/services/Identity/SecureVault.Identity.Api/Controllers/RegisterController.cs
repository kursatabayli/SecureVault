using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVault.Identity.Application.Contracts.DTOs.RegisterDto;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Domain.Entities;

namespace SecureVault.Identity.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RegisterController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto registerUserDto)
        {
            var userInfo = new UserInfo
            {
                Name = registerUserDto.UserInfo.Name,
                Surname = registerUserDto.UserInfo.Surname,
                PhoneNumber = registerUserDto.UserInfo.PhoneNumber
            };

            var command = new RegisterUserCommand(
                registerUserDto.Email,
                registerUserDto.PublicKey,
                registerUserDto.Salt,
                userInfo
            );

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);
            else
                return BadRequest(result);
        }
    }
}
