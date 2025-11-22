using Application.Account.Identity.Services;
using Application.Account.Users.Services;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Account.Users.Events.UserProfileCreated
{
    public class UserProfileCreatedEvent : INotification
    {
        public string? Id { get; set; }
    }

    public class UserProfileCreatedHandler : INotificationHandler<UserProfileCreatedEvent>
    {
        private readonly ILogger<UserProfileCreatedHandler> logger;
        private readonly IUserProfileService userProfileService;
        private readonly IIdentityService identityService;

        public UserProfileCreatedHandler(
            ILogger<UserProfileCreatedHandler> logger,
            IUserProfileService userProfileService,
            IIdentityService identityService)
        {
            this.logger = logger;
            this.userProfileService = userProfileService;
            this.identityService = identityService;
        }

        public async ValueTask Handle(
            UserProfileCreatedEvent notification,
            CancellationToken cancellationToken)
        {
            var userProfileResult = await userProfileService.GetById(
                notification.Id ?? string.Empty,
                cancellationToken);

            if (!userProfileResult.IsSuccess || userProfileResult.Content == null
                || string.IsNullOrWhiteSpace(userProfileResult.Content.Email))
            {
                logger.LogError(
                    "Failed to retrieve user profile for Id: {Id}",
                    notification.Id);

                return;
            }

            logger.LogInformation(
                "User profile retrieved for Id: {Id}",
                userProfileResult.Content.Id);


            // use rabitmq to send event to notify other services that user profile is created

            logger.LogInformation(
                "Initial identity user created successfully with ID: {Id}",
                userProfileResult.Content.Id);
        }
    }
}
