using System.Net;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVault.Identity.Application.Contracts.DTOs.RegisterDto;
using SecureVault.Identity.Application.Features.CQRS.Register.Commands;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RegisterController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RegisterController> _logger;

    public RegisterController(IMediator mediator, ILogger<RegisterController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto registerUserDto)
    {
        _logger.LogInformation("Registration attempt received for email: {Email}", registerUserDto.Email);

        try
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
                userInfo,
                registerUserDto.RecoveryData
            );

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return StatusCode(201);
            }
            else
            {
                _logger.LogWarning("Registration failed for {Email}: {ErrorCode} - {ErrorDescription}",
                    registerUserDto.Email, result.Error.Code, result.Error.Message);

                return BadRequest(result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for email: {Email}", registerUserDto.Email);
            return StatusCode((int)HttpStatusCode.InternalServerError, new Error("InternalError", "An unexpected internal error occurred."));
        }
    }
}
