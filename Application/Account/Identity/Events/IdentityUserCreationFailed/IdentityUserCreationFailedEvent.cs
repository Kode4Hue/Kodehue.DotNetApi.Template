using Mediator;

namespace Application.Account.Identity.Events.IdentityUserCreationFailed
{
    public record IdentityUserCreationFailedEvent(string UserProfileId) : INotification
    {
        public string ErrorMessage { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
