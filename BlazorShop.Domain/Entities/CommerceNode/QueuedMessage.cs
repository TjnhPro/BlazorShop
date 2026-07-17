namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class QueuedMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string TemplateSystemName { get; set; } = string.Empty;

        public Guid? TemplateId { get; set; }

        public string? LanguageCode { get; set; }

        public string ToEmail { get; set; } = string.Empty;

        public string? ToName { get; set; }

        public string FromEmail { get; set; } = string.Empty;

        public string? FromName { get; set; }

        public string? ReplyToEmail { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string BodyHtml { get; set; } = string.Empty;

        public string Status { get; set; } = QueuedMessageStatuses.Pending;

        public int Priority { get; set; }

        public int AttemptCount { get; set; }

        public int MaxAttempts { get; set; } = 3;

        public DateTimeOffset? NextAttemptAtUtc { get; set; }

        public DateTimeOffset? LastAttemptAtUtc { get; set; }

        public DateTimeOffset? SentAtUtc { get; set; }

        public DateTimeOffset? FailedAtUtc { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public string? CorrelationId { get; set; }

        public string? IdempotencyKey { get; set; }

        public string? RelatedEntityType { get; set; }

        public string? RelatedEntityId { get; set; }

        public string? AttachmentMetadataJson { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public CommerceStore? Store { get; set; }

        public MessageTemplate? Template { get; set; }
    }
}
