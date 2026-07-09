namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Domain.Contracts.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeOrderTrackingService : IOrderTrackingService
    {
        private readonly CommerceNodeDbContext context;

        public CommerceNodeOrderTrackingService(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<bool> UpdateTrackingAsync(Guid orderId, string carrier, string trackingNumber, string trackingUrl)
        {
            var order = await this.context.Orders.FirstOrDefaultAsync(item => item.Id == orderId);
            if (order is null)
            {
                return false;
            }

            order.ShippingCarrier = carrier;
            order.TrackingNumber = trackingNumber;
            order.TrackingUrl = trackingUrl;
            order.LastTrackingUpdate = DateTime.UtcNow;
            await this.context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateShippingStatusAsync(
            Guid orderId,
            string shippingStatus,
            DateTime? shippedOn = null,
            DateTime? deliveredOn = null)
        {
            var order = await this.context.Orders.FirstOrDefaultAsync(item => item.Id == orderId);
            if (order is null)
            {
                return false;
            }

            order.ShippingStatus = shippingStatus;
            order.ShippedOn = shippedOn ?? order.ShippedOn;
            order.DeliveredOn = deliveredOn ?? order.DeliveredOn;
            order.LastTrackingUpdate = DateTime.UtcNow;
            await this.context.SaveChangesAsync();

            return true;
        }
    }
}
