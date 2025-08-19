using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace SecureVault.Identity.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserRecoveryDataController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserRecoveryDataController(IMediator mediator)
        {
            _mediator = mediator;
        }

    }
}
