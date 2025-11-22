namespace Domain.Common.Outbox
{
    public class OutboxEntity
    {
        public Guid Id { get; set; }
        public required string Type { get; set; } = default!;   // e.g., "UserRegistrationRequested"
        public required string Payload { get; set; } = default!;
        public int RetryCount { get; set; }
        public Guid? CorrelationId { get; set; }
        public required string Status { get; set; } // dispatched, pending, failed
        public string? Error { get; set; }
        public required DateTime CreatedAtUtc { get; set; } 
        public required DateTime LastUpdatedAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public required DateTime ExpiresAtUtc { get; set; } 
            //= DateTime.UtcNow.AddMinutes(15);
    }
}
