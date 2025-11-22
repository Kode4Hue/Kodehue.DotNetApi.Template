namespace Application.Common.Outbox.DTOs
{
    public class OutboxMessage
    {
        public required string Id { get; set; } = default!;
        public required string Type { get; set; } = default!;
        public required string Payload { get; set; } = default!;
        public bool Dispatched { get; set; }
        public int RetryCount { get; set; }

        public string CorrelationId { get; set; } = default!;
        public required string Status { get; set; } = default!;
        public string? Error { get; set; }

        public required DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
        public required DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedOnUtc { get; set; }
        public required DateTime ExpiresUtc { get; set; } = DateTime.UtcNow.AddMinutes(15);
    }
}
