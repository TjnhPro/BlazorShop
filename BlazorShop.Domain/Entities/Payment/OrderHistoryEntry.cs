namespace BlazorShop.Domain.Entities.Payment
{
    public sealed class OrderHistoryEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public Guid OrderId { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? MetadataJson { get; set; }

        public bool VisibleToCustomer { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public string Source { get; set; } = "system";

        public Order? Order { get; set; }
    }
}
