using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Application.Features.CQRS.Auth.Commands;
using SecureVault.Identity.Application.Features.CQRS.Auth.Handlers;
using SecureVault.Identity.Application.Services;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Tests.Features.CQRS.Auth.Handlers
{
    public class LogoutUserHandlerTests
    {
        private readonly Mock<ITokenValidationService> _tokenValidationServiceMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<LogoutUserHandler>> _loggerMock;
        private readonly LogoutUserHandler _handler;

        public LogoutUserHandlerTests()
        {
            _tokenValidationServiceMock = new Mock<ITokenValidationService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<LogoutUserHandler>>();
            _handler = new LogoutUserHandler(
                _tokenValidationServiceMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_Should_RevokeSessionAndReturnSuccess_WhenTokenIsValid()
        {
            var command = new LogoutUserCommand("access_token", "refresh_token");
            var session = (UserSession)Activator.CreateInstance(typeof(UserSession), true)!;
            var validationResult = Result<UserSession>.Success(session);

            _tokenValidationServiceMock.Setup(s => s.ValidateAndGetSessionAsync(command.AccessToken, command.RefreshToken, null))
                                       .ReturnsAsync(validationResult);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            session.IsRevoked.Should().BeTrue();
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccessWithoutAction_WhenTokenIsInvalid()
        {
            var command = new LogoutUserCommand("invalid_access_token", "invalid_refresh_token");
            var validationError = new Error("Auth.InvalidRefreshToken", "Invalid token");
            var validationResult = Result<UserSession>.Failure(validationError);

            _tokenValidationServiceMock.Setup(s => s.ValidateAndGetSessionAsync(command.AccessToken, command.RefreshToken, null))
                                       .ReturnsAsync(validationResult);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }
    }

}
