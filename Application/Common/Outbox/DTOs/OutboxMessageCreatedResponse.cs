namespace Application.Common.Outbox.DTOs
{
    public class OutboxMessageCreatedResponse
    {
        public string Id { get; set; }
        public string CorrelationId { get; set; }
    }
}
