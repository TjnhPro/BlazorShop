namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.Payment;

    internal static class OrderLifecycleTransitionHelper
    {
        public static void RecordCreated(CommerceNodeDbContext context, Order order)
        {
            OrderHistoryWriter.Add(
                context,
                order,
                "order.created",
                "Order created.",
                newValue: order.OrderStatus,
                visibleToCustomer: true);
        }

        public static void RecordPaymentCaptured(CommerceNodeDbContext context, Order order, string paymentStatus)
        {
            OrderHistoryWriter.Add(
                context,
                order,
                "payment.captured",
                "Payment captured.",
                newValue: paymentStatus,
                visibleToCustomer: true);
        }

        public static void MarkCompleted(CommerceNodeDbContext context, Order order, DateTime now, string source)
        {
            var oldStatus = order.OrderStatus;
            order.OrderStatus = OrderStatuses.Complete;
            order.CompletedAt = now;
            order.UpdatedAt = now;
            OrderHistoryWriter.Add(
                context,
                order,
                "order.completed",
                "Order marked complete.",
                oldStatus,
                order.OrderStatus,
                visibleToCustomer: true,
                source: source);
        }

        public static void MarkCancelled(CommerceNodeDbContext context, Order order, DateTime now, string source)
        {
            var oldStatus = order.OrderStatus;
            order.OrderStatus = OrderStatuses.Cancelled;
            order.CancelledAt = now;
            order.UpdatedAt = now;
            OrderHistoryWriter.Add(
                context,
                order,
                "order.cancelled",
                "Order cancelled.",
                oldStatus,
                order.OrderStatus,
                visibleToCustomer: true,
                source: source);
        }

        public static void UpdateShippingStatus(
            CommerceNodeDbContext context,
            Order order,
            string shippingStatus,
            string source)
        {
            var oldShippingStatus = order.ShippingStatus;
            order.ShippingStatus = ShippingStatusNormalizer.NormalizeOrOriginal(shippingStatus);
            OrderHistoryWriter.Add(
                context,
                order,
                "shipping_status.updated",
                "Order shipping status updated.",
                oldShippingStatus,
                order.ShippingStatus,
                visibleToCustomer: true,
                source: source);
        }

        public static void RecordTrackingUpdated(
            CommerceNodeDbContext context,
            Order order,
            string? oldTrackingNumber,
            string source)
        {
            OrderHistoryWriter.Add(
                context,
                order,
                "tracking.updated",
                "Order tracking updated.",
                oldTrackingNumber,
                order.TrackingNumber,
                visibleToCustomer: true,
                source: source);
        }

        public static void RecordAdminNoteUpdated(CommerceNodeDbContext context, Order order)
        {
            OrderHistoryWriter.Add(
                context,
                order,
                "admin_note.updated",
                "Admin note updated.",
                metadataJson: JsonSerializer.Serialize(new { HasNote = !string.IsNullOrWhiteSpace(order.AdminNote) }),
                source: "admin");
        }
    }
}
