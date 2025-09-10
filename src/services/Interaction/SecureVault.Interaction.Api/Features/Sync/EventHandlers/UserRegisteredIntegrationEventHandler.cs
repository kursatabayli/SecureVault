//using MediatR;
//using Microsoft.Extensions.Logging;
//using SecureVault.Shared.Contracts.Events;
//using SecureVault.Sync.Application.Contracts.Messaging;
//using SecureVault.Sync.Application.Features.CQRS.UserSyncStates.Commands;

//namespace SecureVault.Sync.Application.Features.IntegrationEventHandlers
//{
//    public class UserRegisteredIntegrationEventHandler : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
//    {
//        private readonly IMediator _mediator;
//        private readonly ILogger<UserRegisteredIntegrationEventHandler> _logger;

//        public UserRegisteredIntegrationEventHandler(IMediator mediator, ILogger<UserRegisteredIntegrationEventHandler> logger)
//        {
//            _mediator = mediator;
//            _logger = logger;
//        }

//        public async Task Handle(UserRegisteredIntegrationEvent @event)
//        {
//            _logger.LogInformation("Gelen entegrasyon olayı işleniyor: UserId {UserId}", @event.UserId);

//            var command = new CreateUserSyncStatesCommand(@event.UserId);
//            var result = await _mediator.Send(command);

//            if (!result.IsSuccess)
//            {
//                var errorMessage = $"Entegrasyon olayı işlenemedi: {result.Error.Message}";
//                _logger.LogError(errorMessage);
//                throw new InvalidOperationException(errorMessage);
//            }
//        }
//    }
//}
