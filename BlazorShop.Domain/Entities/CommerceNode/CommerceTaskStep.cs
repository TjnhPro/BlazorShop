namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class CommerceTaskStep
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TaskId { get; set; }

        public CommerceTask? Task { get; set; }

        public string StepKey { get; set; } = string.Empty;

        public string Status { get; set; } = CommerceTaskStepStatuses.Pending;

        public int AttemptNumber { get; set; }

        public string? ResultJson { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTimeOffset? StartedAt { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }
    }
}
