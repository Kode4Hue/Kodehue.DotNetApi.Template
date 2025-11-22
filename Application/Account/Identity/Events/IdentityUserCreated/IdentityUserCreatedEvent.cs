using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Account.Identity.Events.IdentityUserCreated
{
    public class IdentityUserCreatedEvent : INotification
    {
        public string? IdentityUserId { get; }

        private IdentityUserCreatedEvent(string? identityUserId)
        {
            IdentityUserId = identityUserId;
        }

        public static IdentityUserCreatedEvent Create(string? identityUserId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identityUserId);
            return new IdentityUserCreatedEvent(identityUserId);
        }
    }

    public class IdentityUserCreatedHandler : INotificationHandler<IdentityUserCreatedEvent>
    {

        private readonly ILogger<IdentityUserCreatedHandler> logger;

        public IdentityUserCreatedHandler(ILogger<IdentityUserCreatedHandler> logger)
        {
            this.logger = logger;
        }
        public async ValueTask Handle(
            IdentityUserCreatedEvent notification,
            CancellationToken cancellationToken)
        {
            // No operation (no-op) handler
            await Task.CompletedTask;
        }
    }
}
