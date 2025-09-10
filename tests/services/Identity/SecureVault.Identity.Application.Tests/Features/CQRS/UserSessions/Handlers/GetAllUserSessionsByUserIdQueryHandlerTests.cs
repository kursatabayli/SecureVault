using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using SecureVault.Identity.Application.Contracts.Repositories;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Handlers;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Queries;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Results;
using SecureVault.Identity.Application.Messages;
using SecureVault.Identity.Domain.Entities;

namespace SecureVault.Identity.Application.Tests.Features.CQRS.UserSessions.Handlers
{
    public class GetAllUserSessionsByUserIdQueryHandlerTests
    {
        private readonly Mock<IUserSessionRepository> _userSessionRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly GetAllUserSessionsByUserIdQueryHandler _handler;

        public GetAllUserSessionsByUserIdQueryHandlerTests()
        {
            _userSessionRepositoryMock = new Mock<IUserSessionRepository>();
            _mapperMock = new Mock<IMapper>();
            _handler = new GetAllUserSessionsByUserIdQueryHandler(
                _userSessionRepositoryMock.Object,
                _mapperMock.Object,
                Mock.Of<ILogger<GetAllUserSessionsByUserIdQueryHandler>>(),
                Mock.Of<IStringLocalizer<ReturnMessages>>());
        }

        [Fact]
        public async Task Handle_Should_ReturnMappedSessions_WhenSessionsExist()
        {
            var query = new GetAllUserSessionsByUserIdQuery(Guid.NewGuid());
            var sessionsFromRepo = new List<UserSession> { (UserSession)Activator.CreateInstance(typeof(UserSession), true)! };
            var mappedResults = new List<UserSessionResult> { new UserSessionResult() };

            _userSessionRepositoryMock.Setup(r => r.GetAllSessionsByUserIdAsync(query.UserId))
                                      .ReturnsAsync(sessionsFromRepo);

            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<UserSessionResult>>(sessionsFromRepo))
                       .Returns(mappedResults);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(mappedResults);
        }
    }

}
