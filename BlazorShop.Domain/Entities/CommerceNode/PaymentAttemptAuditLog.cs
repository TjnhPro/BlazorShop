namespace BlazorShop.Domain.Entities.CommerceNode
{
    using BlazorShop.Domain.Entities.Payment;

    public sealed class PaymentAttemptAuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public Guid? OrderId { get; set; }

        public Guid PaymentAttemptId { get; set; }

        public string ProviderKey { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public string? OldState { get; set; }

        public string NewState { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? MetadataJson { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public CommerceStore? Store { get; set; }

        public PaymentAttempt? PaymentAttempt { get; set; }

        public Order? Order { get; set; }
    }
}
