using Application.Account.Identity.Services;
using Application.Account.Users.Services;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Account.Users.Requests.SignupAttendee
{
    public sealed class AttendeeUserProfileCreatedEvent : INotification
    {
        public string UserProfileId { get; }

        private AttendeeUserProfileCreatedEvent(string userProfileId)
        {
            UserProfileId = userProfileId;
        }

        public static AttendeeUserProfileCreatedEvent Create(
            string userProfileId)
        {
            return new AttendeeUserProfileCreatedEvent(userProfileId);
        }
    }


    public class AttendeeUserProfileCreatedHandler : INotificationHandler<AttendeeUserProfileCreatedEvent>
    {
        private readonly ILogger<AttendeeUserProfileCreatedHandler> logger;
        private readonly IUserProfileService userProfileService;
        private readonly IIdentityService identityService;

        public AttendeeUserProfileCreatedHandler(
            ILogger<AttendeeUserProfileCreatedHandler> logger,
            IUserProfileService userProfileService,
            IIdentityService identityService)
        {
            this.logger = logger;
            this.userProfileService = userProfileService;
            this.identityService = identityService;
        }

        public async ValueTask Handle(
            AttendeeUserProfileCreatedEvent notification,
            CancellationToken cancellationToken)
        {

            var userProfileResult = await userProfileService.GetById(
                notification.UserProfileId, cancellationToken);

            if (!userProfileResult.IsSuccess || userProfileResult.Content == null)
            {
                logger.LogError("User profile with ID {UserProfileId} not found.",
                    notification.UserProfileId);
                return;
            }

            await Task.CompletedTask;
        }
    }
}
