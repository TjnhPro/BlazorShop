namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class CommerceTask
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public string TaskType { get; set; } = string.Empty;

        public string Status { get; set; } = CommerceTaskStatuses.Pending;

        public string? IdempotencyKey { get; set; }

        public string? LockKey { get; set; }

        public string PayloadSchemaVersion { get; set; } = "v1";

        public string PayloadJson { get; set; } = "{}";

        public string? ResultJson { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public int AttemptCount { get; set; }

        public int MaxAttempts { get; set; } = 3;

        public DateTimeOffset? NextAttemptAt { get; set; }

        public DateTimeOffset? StartedAt { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string? CreatedBy { get; set; }

        public string? CorrelationId { get; set; }

        public DateTimeOffset? CancelRequestedAt { get; set; }

        public string? CancelReason { get; set; }

        public string? WorkerId { get; set; }

        public DateTimeOffset? LastHeartbeatAt { get; set; }

        public ICollection<CommerceTaskStep> Steps { get; set; } = new List<CommerceTaskStep>();
    }
}
