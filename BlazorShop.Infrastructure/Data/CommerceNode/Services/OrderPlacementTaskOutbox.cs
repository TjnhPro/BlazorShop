namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    internal static class OrderPlacementTaskOutbox
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public static async Task AddOrderCreatedTaskAsync(
            CommerceNodeDbContext context,
            Order order,
            CancellationToken cancellationToken)
        {
            var idempotencyKey = BuildOrderCreatedIdempotencyKey(order.Id);
            var trackedExists = context.ChangeTracker
                .Entries<CommerceTask>()
                .Any(entry => string.Equals(entry.Entity.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));
            if (trackedExists)
            {
                return;
            }

            var persistedExists = await context.CommerceTasks
                .AsNoTracking()
                .AnyAsync(task => task.IdempotencyKey == idempotencyKey, cancellationToken);
            if (persistedExists)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            context.CommerceTasks.Add(new CommerceTask
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TaskType = OrderPlacementTaskTypes.OrderCreated,
                Status = CommerceTaskStatuses.Pending,
                IdempotencyKey = idempotencyKey,
                LockKey = $"order:{order.Id:N}",
                PayloadSchemaVersion = "v1",
                PayloadJson = JsonSerializer.Serialize(
                    new
                    {
                        orderId = order.Id,
                        storeId = order.StoreId,
                        reference = order.Reference,
                        customerEmail = order.CustomerEmail,
                        totalAmount = order.TotalAmount,
                        currencyCode = order.CurrencyCode,
                        createdAtUtc = order.CreatedOn,
                    },
                    JsonOptions),
                AttemptCount = 0,
                MaxAttempts = 3,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "order-placement",
                CorrelationId = order.Reference,
            });
        }

        private static string BuildOrderCreatedIdempotencyKey(Guid orderId)
        {
            return $"{OrderPlacementTaskTypes.OrderCreated}:{orderId:N}";
        }
    }
}
