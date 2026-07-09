namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Contracts.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeOrderTrackingService : IOrderTrackingService
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;

        public CommerceNodeOrderTrackingService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext)
        {
            this.context = context;
            this.storeContext = storeContext;
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

            order.ShippingStatus = shippingStatus;
            order.ShippedOn = shippedOn ?? order.ShippedOn;
            order.DeliveredOn = deliveredOn ?? order.DeliveredOn;
            order.LastTrackingUpdate = DateTime.UtcNow;
            await this.context.SaveChangesAsync();

            return true;
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync();
            return result.Success ? result.Payload : null;
        }
    }
}
