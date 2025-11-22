using Application.Common.Outbox.DTOs;
using Application.Common.Results;

namespace Application.Common.Outbox.Services
{
    public interface IOutboxService
    {
        Task<Result<OutboxMessageCreatedResponse>> AddEventAsync(
            NewOutboxMessage outboxMessage, CancellationToken cancellationToken = default);
        Task<List<OutboxMessage>> GetPendingEventsAsync(CancellationToken cancellationToken = default);
        Task<OutboxMessage?> GetEventByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<NoContentResult> ClearPendingEventsAsync(CancellationToken cancellationToken = default);
        Task<Result<OutboxMessage>> UpdateEventAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken = default);
        Task<NoContentResult> DeleteEventAsync(string id, CancellationToken cancellationToken = default);
    }
}
