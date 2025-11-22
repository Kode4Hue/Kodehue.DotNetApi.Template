using Application.Account.Identity.DTOs;
using Application.Account.Identity.Services;
using Application.Account.Users.Requests.SignupAttendee.DTOs;
using Application.Account.Users.Services;
using Application.Common.Outbox.DTOs;
using Application.Common.Outbox.Services;
using Application.Common.Results;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Account.Users.Requests.SignupAttendee
{
    public class SignupAttendeeCommand : IRequest<Result<SignupAttendeeResponse>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class SignupAttendeeHandler : IRequestHandler<SignupAttendeeCommand, Result<SignupAttendeeResponse>>
    {
        //private readonly IMediator Mediator;
        private readonly ILogger<SignupAttendeeHandler> logger;
        private readonly IIdentityService identityService;
        private readonly IUserProfileService userProfileService;
        private readonly IOutboxService outboxService;

        public SignupAttendeeHandler(
            ILogger<SignupAttendeeHandler> logger,
            IIdentityService identityService,
            IUserProfileService userProfileService,
            IOutboxService outboxService
            )
        {
            this.logger = logger;
            this.userProfileService = userProfileService;
            this.identityService = identityService;
            this.outboxService = outboxService;
        }

        public async ValueTask<Result<SignupAttendeeResponse>> Handle(
            SignupAttendeeCommand request,
            CancellationToken cancellationToken)
        {

            //create identity in identity service
            var identityUserResult = await identityService
                .CreateInitialIdentityUserAsync(
                    request.Email,
                    request.Password,
                    cancellationToken);

            if (!identityUserResult.IsSuccess || identityUserResult.Content is null)
            {
                logger.LogError("Failed to create identity user.");
                return Result<SignupAttendeeResponse>.CreateErrorResult(
                    identityUserResult.Errors,
                    identityUserResult.ErrorType);
            }

            var identityUserId = identityUserResult.Content.Id;

            var createUserProfileResult = await userProfileService
                .CreateInitalUserProfile(
                    email: request.Email,
                    identityUserId: identityUserId,
                    emailVerified: false,
                    cancellationToken);

            if (!createUserProfileResult.IsSuccess)
            {
                
                logger.LogWarning("UserProfile creation failed. Rolling back Keycloak user {identityUserId}", identityUserId);

                // COMPENSATE
                var deleteResult = await identityService.DeleteIdentityUserAsync(identityUserId, cancellationToken);

                if (!deleteResult.IsSuccess)
                {
                    logger.LogError("ROLLBACK FAILED. Keycloak user {identityUserId} may be orphaned", identityUserId);
                    // Log to Dead Letter or Monitoring
                }

                return Result<SignupAttendeeResponse>.CreateErrorResult(
                    createUserProfileResult.Errors,
                    createUserProfileResult.ErrorType);
            }

            if (string.IsNullOrWhiteSpace(createUserProfileResult?.Content?.Id))
            {
                logger.LogError("User profile creation returned invalid/empty ID.");
                return Result<SignupAttendeeResponse>.CreateErrorResult(
                    new List<ErrorDto> { ErrorDto.CreateFromMessage(
                        "User profile creation failed.") },
                    ErrorType.ApplicationError);
            }

            // create mediator event and save to outbox
            var attendeeUserProfileCreatedEvent = AttendeeUserProfileCreatedEvent.Create(
            userProfileId: createUserProfileResult.Content.Id);

            var newOutboxMessage = new NewOutboxMessage
            {
                Type = nameof(AttendeeUserProfileCreatedEvent),
                Payload = JsonSerializer.Serialize(attendeeUserProfileCreatedEvent)
            };

            var result = await outboxService.AddEventAsync(
                newOutboxMessage);

            if (!result.IsSuccess || result.Content == null)
            {
                logger.LogError("Failed to add event to outbox.");

                return Result<SignupAttendeeResponse>.CreateErrorResult(
                    new List<ErrorDto> { ErrorDto.CreateFromMessage(
                            "An unexpected error occurred while adding AttendeeUserProfileCreatedEvent to outbox.") },
                    ErrorType.ApplicationError);
            }

            var response = new SignupAttendeeResponse(
                UserProfileId: createUserProfileResult.Content.Id);

            logger.LogInformation("User profile created successfully with ID: {UserProfileId}", response.UserProfileId);

            return Result<SignupAttendeeResponse>.CreateSuccessResult(response);
        }
    }

    public sealed class SignupAttendeeCommandValidator : AbstractValidator<SignupAttendeeCommand>
    {
        public SignupAttendeeCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
        }
    }
}
