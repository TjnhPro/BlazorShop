namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts.Payment;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeOrderTrackingService : IOrderTrackingService
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly ICommerceTransactionalMessageService transactionalMessageService;

        public CommerceNodeOrderTrackingService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            ICommerceTransactionalMessageService transactionalMessageService)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.transactionalMessageService = transactionalMessageService;
        }

        public async Task<bool> UpdateTrackingAsync(Guid orderId, string carrier, string trackingNumber, string trackingUrl)
        {
            var storeId = await this.ResolveCurrentStoreIdAsync();
            if (!storeId.HasValue)
            {
                return false;
            }

            var order = await this.context.Orders.FirstOrDefaultAsync(item => item.Id == orderId && item.StoreId == storeId);
            if (order is null)
            {
                return false;
            }

            var oldTrackingNumber = order.TrackingNumber;
            order.ShippingCarrier = carrier;
            order.TrackingNumber = trackingNumber;
            order.TrackingUrl = trackingUrl;
            order.LastTrackingUpdate = DateTime.UtcNow;

            var shipment = await this.context.Shipments
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.OrderId == order.Id && item.StoreId == storeId);
            if (shipment is not null)
            {
                this.context.ShipmentTrackingEvents.Add(new ShipmentTrackingEvent
                {
                    ShipmentId = shipment.Id,
                    StoreId = storeId.Value,
                    OrderId = order.Id,
                    Status = "tracking_updated",
                    Message = "Order tracking updated.",
                    OccurredAtUtc = order.LastTrackingUpdate.Value,
                    Source = "manual_admin",
                    CreatedAt = order.LastTrackingUpdate.Value,
                });
            }

            OrderLifecycleTransitionHelper.RecordTrackingUpdated(
                this.context,
                order,
                oldTrackingNumber,
                source: "manual_admin");
            await this.context.SaveChangesAsync();
            await this.TryQueueFulfillmentStatusChangedAsync(storeId.Value, order.Id);

            return true;
        }

        public async Task<bool> UpdateShippingStatusAsync(
            Guid orderId,
            string shippingStatus,
            DateTime? shippedOn = null,
            DateTime? deliveredOn = null)
        {
            var storeId = await this.ResolveCurrentStoreIdAsync();
            if (!storeId.HasValue)
            {
                return false;
            }

            var order = await this.context.Orders.FirstOrDefaultAsync(item => item.Id == orderId && item.StoreId == storeId);
            if (order is null)
            {
                return false;
            }

            var normalizedShippingStatus = ShippingStatusNormalizer.NormalizeOrOriginal(shippingStatus);
            OrderLifecycleTransitionHelper.UpdateShippingStatus(
                this.context,
                order,
                normalizedShippingStatus,
                source: "manual_admin");
            order.ShippedOn = shippedOn.HasValue ? EnsureUtc(shippedOn.Value) : order.ShippedOn;
            order.DeliveredOn = deliveredOn.HasValue ? EnsureUtc(deliveredOn.Value) : order.DeliveredOn;
            order.LastTrackingUpdate = DateTime.UtcNow;

            if (string.Equals(normalizedShippingStatus, ShippingStatuses.Delivered, StringComparison.Ordinal))
            {
                var shipment = await this.context.Shipments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.OrderId == order.Id && item.StoreId == storeId);
                if (shipment is not null)
                {
                    var occurredAt = order.DeliveredOn ?? order.LastTrackingUpdate.Value;
                    this.context.ShipmentTrackingEvents.Add(new ShipmentTrackingEvent
                    {
                        ShipmentId = shipment.Id,
                        StoreId = storeId.Value,
                        OrderId = order.Id,
                        Status = "delivered",
                        Message = "Shipment delivered.",
                        OccurredAtUtc = occurredAt,
                        Source = "manual_admin",
                        CreatedAt = order.LastTrackingUpdate.Value,
                    });
                }
            }

            await this.context.SaveChangesAsync();
            await this.TryQueueFulfillmentStatusChangedAsync(storeId.Value, order.Id);

            return true;
        }

        private async Task TryQueueFulfillmentStatusChangedAsync(Guid storeId, Guid orderId)
        {
            try
            {
                await this.transactionalMessageService.QueueFulfillmentStatusChangedAsync(storeId, orderId);
            }
            catch
            {
                // Notification delivery is asynchronous and must not roll back fulfillment updates.
            }
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync();
            return result.Success ? result.Payload : null;
        }
    }
}
