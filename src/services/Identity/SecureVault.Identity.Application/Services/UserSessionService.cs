using Microsoft.Extensions.Logging;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Domain.Entities;

namespace SecureVault.Identity.Application.Services;

public class UserSessionService : IUserSessionService
{
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly ILogger<UserSessionService> _logger;
    public UserSessionService(IUserSessionRepository userSessionRepository, ILogger<UserSessionService> logger)
    {
        _userSessionRepository = userSessionRepository;
        _logger = logger;
    }

    public async Task ManageSessionAsync(User user, LoginUserCommand request, string accessTokenJti, string refreshTokenJti, DateTime refreshTokenExpiration)
    {
        var deviceDetails = new DeviceDetail
        {
            UniqueDeviceId = request.UniqueDeviceId,
            DeviceName = request.DeviceName,
            DeviceModel = request.DeviceModel,
            DeviceManufacturer = request.DeviceManufacturer,
            OperatingSystem = request.OperatingSystem,
        };

        var existingSession = await _userSessionRepository.IsDeviceExistAsync(user.Id, deviceDetails.UniqueDeviceId);

        if (existingSession is null)
        {
            _logger.LogInformation("Creating new session for User: {UserId}, Device: {DeviceId}", user.Id, deviceDetails.UniqueDeviceId);
            var newSession = UserSession.Create(
                userId: user.Id,
                accessTokenJti: accessTokenJti,
                refreshTokenJti: refreshTokenJti,
                deviceDetails: deviceDetails,
                ipAddress: request.IpAddress,
                expiresAt: refreshTokenExpiration,
                isRevoked: false,
                isPersistent: request.RememberMe
            );
            try
            {
                await _userSessionRepository.AddAsync(newSession);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add new session for User: {UserId}, Device: {DeviceId}", user.Id, deviceDetails.UniqueDeviceId);
                throw;
            }
        }
        else
        {
            _logger.LogInformation("Updating existing session for User: {UserId}, Device: {DeviceId}", user.Id, deviceDetails.UniqueDeviceId);

            existingSession.Update(
                refreshTokenJti: refreshTokenJti,
                accessTokenJti: accessTokenJti,
                ipAddress: request.IpAddress,
                expiresAt: refreshTokenExpiration,
                isPersistent: request.RememberMe
            );
        }
    }
}
