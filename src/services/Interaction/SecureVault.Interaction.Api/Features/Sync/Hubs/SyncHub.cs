using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SecureVault.Interaction.Api.Features.Sync.Contracts;
using System.Security.Claims;

namespace SecureVault.Interaction.Api.Features.Sync.Hubs
{
    [Authorize]
    public class SyncHub : Hub
    {
        private readonly IUserConnectionManager _userConnectionManager;
        private readonly ILogger<SyncHub> _logger;

        public SyncHub(IUserConnectionManager userConnectionManager, ILogger<SyncHub> logger)
        {
            _userConnectionManager = userConnectionManager;
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            var userId = GetUserIdFromContext();
            var deviceId = GetDeviceIdFromContext();
            var connectionId = Context.ConnectionId; 
            _userConnectionManager.AddConnection(userId, connectionId, deviceId);
            _logger.LogInformation("Kullanıcı bağlandı. UserId: {UserId}, ConnectionId: {ConnectionId}", userId, connectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            var userId = _userConnectionManager.RemoveConnection(connectionId);

            if (userId.HasValue)
            {
                if (exception is not null)
                {
                    _logger.LogWarning(exception, "Kullanıcı bağlantısı bir hata ile kesildi. UserId: {UserId}, ConnectionId: {ConnectionId}", userId.Value, connectionId);
                }
                else
                {
                    _logger.LogInformation("Kullanıcı bağlantısı kesildi. UserId: {UserId}, ConnectionId: {ConnectionId}", userId.Value, connectionId);
                }
            }
            else
            {
                _logger.LogWarning("Takip edilmeyen bir bağlantı kesildi. ConnectionId: {ConnectionId}", connectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        private Guid GetUserIdFromContext()
        {
            var userIdClaim = Context.User?.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new InvalidOperationException("Geçerli bir kullanıcı kimliği bulunamadı.");
            }
            return userId;
        }

        private string GetDeviceIdFromContext()
        {
            var httpContext = Context.GetHttpContext();
            var deviceId = httpContext?.Request.Headers["X-Device-Id"].ToString();

            if (string.IsNullOrEmpty(deviceId))
            {
                throw new InvalidOperationException("Geçerli bir cihaz kimliği ('X-Device-ID' header) bulunamadı.");
            }
            return deviceId;
        }
    }
}
