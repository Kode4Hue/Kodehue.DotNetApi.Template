namespace Application.Common.Outbox.DTOs
{
    public class NewOutboxMessage
    {
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string Type { get; set; } = default!;
        public string Payload { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string? CorrelationId { get; set; }
    }
}
