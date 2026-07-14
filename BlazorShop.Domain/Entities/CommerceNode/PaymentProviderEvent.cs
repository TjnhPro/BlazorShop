namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class PaymentProviderEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public Guid? PaymentAttemptId { get; set; }

        public string ProviderKey { get; set; } = string.Empty;

        public string? EventId { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string PayloadHash { get; set; } = string.Empty;

        public string PayloadJson { get; set; } = string.Empty;

        public DateTimeOffset? ProcessedAtUtc { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public CommerceStore? Store { get; set; }

        public PaymentAttempt? PaymentAttempt { get; set; }
    }
}
