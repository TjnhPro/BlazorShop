namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeOrderQueryService : IOrderQueryService
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;

        public CommerceNodeOrderQueryService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext)
        {
            this.context = context;
            this.storeContext = storeContext;
        }

        public async Task<IEnumerable<GetOrder>> GetOrdersForUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return [];
            }

            var orderQuery = await this.GetCurrentStoreOrdersAsync();
            var orders = await orderQuery
                .Where(order => order.UserId == userId)
                .OrderByDescending(order => order.CreatedOn)
                .ToListAsync();

            return await this.MapOrdersAsync(orders);
        }

        public async Task<IEnumerable<GetOrder>> GetAllAsync()
        {
            var orderQuery = await this.GetCurrentStoreOrdersAsync();
            var orders = await orderQuery
                .OrderByDescending(order => order.CreatedOn)
                .ToListAsync();

            return await this.MapOrdersAsync(orders);
        }

        private async Task<IQueryable<Order>> GetCurrentStoreOrdersAsync()
        {
            var storeId = await this.ResolveCurrentStoreIdAsync();
            return storeId.HasValue
                ? this.context.Orders
                    .AsNoTracking()
                    .Include(order => order.Lines)
                    .Where(order => order.StoreId == storeId.Value)
                : this.context.Orders.AsNoTracking().Where(order => false);
        }

        private async Task<IReadOnlyList<GetOrder>> MapOrdersAsync(IReadOnlyCollection<Order> orders)
        {
            if (orders.Count == 0)
            {
                return [];
            }

            var productIds = orders.SelectMany(order => order.Lines).Select(line => line.ProductId).Distinct().ToArray();
            var productNames = await this.context.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .Select(product => new { product.Id, product.Name })
                .ToDictionaryAsync(product => product.Id, product => product.Name ?? string.Empty);

            var orderIds = orders.Select(order => order.Id).ToArray();
            var trackingEvents = await this.context.ShipmentTrackingEvents
                .AsNoTracking()
                .Where(trackingEvent => orderIds.Contains(trackingEvent.OrderId))
                .OrderBy(trackingEvent => trackingEvent.OccurredAtUtc)
                .Select(trackingEvent => new
                {
                    trackingEvent.OrderId,
                    Event = new GetShipmentTrackingEvent
                    {
                        Id = trackingEvent.Id,
                        Status = trackingEvent.Status,
                        Message = trackingEvent.Message,
                        OccurredAtUtc = trackingEvent.OccurredAtUtc,
                        Location = trackingEvent.Location,
                        Source = trackingEvent.Source,
                    },
                })
                .ToListAsync();

            var trackingEventsByOrder = trackingEvents
                .GroupBy(item => item.OrderId)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Event).ToArray());

            return orders.Select(order => new GetOrder
            {
                Id = order.Id,
                Reference = order.Reference,
                Status = order.OrderStatus,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                PaymentMethodKey = order.PaymentMethodKey,
                PaymentAt = order.PaymentAt,
                StoreSnapshot = OrderSnapshotProjection.ToStoreSnapshot(order),
                CurrencyCode = order.CurrencyCode,
                TotalAmount = order.TotalAmount,
                TotalBreakdown = OrderSnapshotProjection.ToTotalBreakdown(
                    order.SubtotalAmount,
                    order.ShippingTotalAmount,
                    order.TaxTotalAmount,
                    order.DiscountTotalAmount,
                    order.GrandTotalAmount),
                BaseCurrencyCode = order.BaseCurrencyCode,
                BaseTotalAmount = order.BaseTotalAmount,
                BaseTotalBreakdown = OrderSnapshotProjection.ToTotalBreakdown(
                    order.BaseSubtotalAmount,
                    order.BaseShippingTotalAmount,
                    order.BaseTaxTotalAmount,
                    order.BaseDiscountTotalAmount,
                    order.BaseGrandTotalAmount),
                ExchangeRate = order.ExchangeRate,
                ExchangeRateProviderKey = order.ExchangeRateProviderKey,
                ExchangeRateSource = order.ExchangeRateSource,
                ExchangeRateEffectiveAtUtc = order.ExchangeRateEffectiveAtUtc,
                ExchangeRateExpiresAtUtc = order.ExchangeRateExpiresAtUtc,
                CreatedOn = order.CreatedOn,
                ShippingStatus = order.ShippingStatus,
                ShippingCarrier = order.ShippingCarrier,
                TrackingNumber = order.TrackingNumber,
                TrackingUrl = order.TrackingUrl,
                ShippedOn = order.ShippedOn,
                DeliveredOn = order.DeliveredOn,
                UserId = order.UserId,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                BillingAddress = OrderSnapshotProjection.ToAddress(order.BillingAddressSnapshotJson),
                ShippingAddressSnapshot = OrderSnapshotProjection.ToShippingAddressSnapshot(order),
                ShippingFullName = order.ShippingFullName,
                ShippingEmail = order.ShippingEmail,
                ShippingPhone = order.ShippingPhone,
                ShippingAddress1 = order.ShippingAddress1,
                ShippingAddress2 = order.ShippingAddress2,
                ShippingCity = order.ShippingCity,
                ShippingState = order.ShippingState,
                ShippingPostalCode = order.ShippingPostalCode,
                ShippingCountryCode = order.ShippingCountryCode,
                ShippingMethod = OrderSnapshotProjection.ToShippingMethod(order),
                CompletedAt = order.CompletedAt,
                CancelledAt = order.CancelledAt,
                AdminNote = order.AdminNote,
                TrackingEvents = trackingEventsByOrder.TryGetValue(order.Id, out var events) ? events : [],
                Lines = order.Lines.Select(line => new GetOrderLine
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    CurrencyCode = line.CurrencyCode,
                    BaseUnitPrice = line.BaseUnitPrice,
                    ConvertedUnitPrice = line.ConvertedUnitPrice,
                    PersistedLineTotal = line.LineTotal,
                    BaseLineTotal = line.BaseLineTotal,
                    ProductName = line.ProductName ?? (productNames.TryGetValue(line.ProductId, out var productName) ? productName : string.Empty),
                    Sku = line.Sku,
                    Image = line.Image,
                    ProductVariantId = line.ProductVariantId,
                    VariantAttributes = ProductVariantAttributeNormalizer.Deserialize(line.VariantAttributesJson),
                }),
            }).ToArray();
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync();
            return result.Success ? result.Payload : null;
        }
    }
}
