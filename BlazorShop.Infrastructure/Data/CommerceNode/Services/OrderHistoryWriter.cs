namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Domain.Entities.Payment;

    internal static class OrderHistoryWriter
    {
        public static void Add(
            CommerceNodeDbContext context,
            Order order,
            string eventType,
            string message,
            string? oldValue = null,
            string? newValue = null,
            string? metadataJson = null,
            bool visibleToCustomer = false,
            string source = "system")
        {
            if (!order.StoreId.HasValue)
            {
                return;
            }

            context.OrderHistoryEntries.Add(new OrderHistoryEntry
            {
                Id = Guid.NewGuid(),
                StoreId = order.StoreId.Value,
                OrderId = order.Id,
                EventType = eventType,
                OldValue = Normalize(oldValue, 128),
                NewValue = Normalize(newValue, 128),
                Message = Normalize(message, 512) ?? eventType,
                MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson,
                VisibleToCustomer = visibleToCustomer,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Source = Normalize(source, 64) ?? "system",
            });
        }

        private static string? Normalize(string? value, int maxLength)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            return normalized is null || normalized.Length <= maxLength ? normalized : normalized[..maxLength];
        }
    }
}
