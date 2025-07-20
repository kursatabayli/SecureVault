using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Domain.Entities;

namespace SecureVault.Identity.Application.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IUserSessionRepository _userSessionRepository;

        public UserSessionService(IUserSessionRepository userSessionRepository)
        {
            _userSessionRepository = userSessionRepository;
        }

        public async Task ManageSessionAsync(User user, LoginUserCommand request, string newTokenIdentifier, DateTime newRefreshTokenExpiration)
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
                var newSession = UserSession.Create(
                    userId: user.Id,
                    tokenIdentifier: newTokenIdentifier,
                    deviceDetails: deviceDetails,
                    ipAddress: request.IpAddress,
                    expiresAt: newRefreshTokenExpiration,
                    isRevoked: false,
                    isPersistent: request.RememberMe
                );
                await _userSessionRepository.AddAsync(newSession);
            }
            else
            {
                existingSession.Update(
                    tokenIdentifier: newTokenIdentifier,
                    ipAddress: request.IpAddress,
                    expiresAt: newRefreshTokenExpiration,
                    isPersistent: request.RememberMe
                );
            }
        }
    }
}
