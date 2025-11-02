using Microsoft.AspNetCore.Mvc;
using SecureVault.Interaction.Api.Features.QrLogin.Contracts;

namespace SecureVault.Interaction.Api.Features.QrLogin;

[ApiController]
[Route("api/[controller]")]
public class QrLoginController : ControllerBase
{
    private readonly IQrLoginChannelService _channelService;

    public QrLoginController(IQrLoginChannelService channelService)
    {
        _channelService = channelService;
    }

    [HttpPost("create-channel")]
    public async Task<IActionResult> CreateChannel()
    {
        var channelId = await _channelService.CreateChannelAsync();
        return Ok(new { ChannelId = channelId });
    }
}
