using Application.Account.Users.Requests.SignupAttendee;
using Application.Common.Outbox.Services;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Text.Json;

namespace Infrastructure.Account.Users.BackgroundServices
{

    [DisallowConcurrentExecution]
    public class UserRegistrationOutboxMessageProcessor : IJob
    {
        private ILogger<UserRegistrationOutboxMessageProcessor> logger;
        private readonly IOutboxService outboxService;
        private IConfiguration configuration;
        IPublisher publisher;

        public UserRegistrationOutboxMessageProcessor(
            ILogger<UserRegistrationOutboxMessageProcessor> logger,
            IOutboxService outboxService,
            IConfiguration configuration,
            IPublisher publisher)
        {
            this.logger = logger;
            this.outboxService = outboxService;
            this.configuration = configuration;
            this.publisher = publisher;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var messages = await outboxService.GetPendingEventsAsync();

            foreach (var message in messages)
            {
                var attendeeUserProfileCreatedEvent =
                        JsonSerializer.Deserialize<AttendeeUserProfileCreatedEvent>(
                            message.Payload);

                var dateTime = DateTime.UtcNow;

                if (attendeeUserProfileCreatedEvent == null)
                {
                    logger.LogError("Outbox Message Payload Deserialization Failed for outbox message with ID {MessageId}", message.Id);
                    message.Error = "Outbox message deserialization failed";
                    message.RetryCount += 1;
                }
                else
                {
                    await publisher.Publish(attendeeUserProfileCreatedEvent);
                    message.ProcessedOnUtc = dateTime; // Updated to use the local variable
                }

                message.LastUpdatedUtc = dateTime; // Updated to use the local variable
                await outboxService.UpdateEventAsync(message);
            }
        }
    }
}
