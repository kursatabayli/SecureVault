using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Queries;
using System.Security.Claims;

namespace SecureVault.Identity.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserSessionController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserSessionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllUserSessionsByUserId()
        {
            var result = await _mediator.Send(new GetAllUserSessionsByUserIdQuery(CurrentUserId));

            if (result.IsSuccess)
                return Ok(result.Value);

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
