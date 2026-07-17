namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.Application.CommerceNode.Orders;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class StorefrontGuestOrderService : IStorefrontGuestOrderService
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;

        public StorefrontGuestOrderService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext)
        {
            this.context = context;
            this.storeContext = storeContext;
        }

        public async Task<ServiceResponse<GetOrder>> GetAsync(
            StorefrontGuestOrderLookupRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var reference = NormalizeNullable(request.Reference);
            var accessToken = NormalizeNullable(request.AccessToken);
            if (reference is null || accessToken is null)
            {
                return Failed("Order reference and access token are required.", ServiceResponseType.ValidationError);
            }

            var storeResult = await this.storeContext.GetCurrentStoreIdAsync();
            if (!storeResult.Success)
            {
                return Failed("Order was not found.", ServiceResponseType.NotFound);
            }

            var tokenHash = ComputeSha256(accessToken);
            var now = DateTimeOffset.UtcNow;
            var order = await this.context.Orders
                .AsNoTracking()
                .Include(item => item.Lines)
                .FirstOrDefaultAsync(
                    item => item.StoreId == storeResult.Payload
                        && item.Reference == reference
                        && item.GuestAccessTokenHash == tokenHash
                        && (!item.GuestAccessTokenExpiresAtUtc.HasValue || item.GuestAccessTokenExpiresAtUtc > now),
                    cancellationToken);
            if (order is null)
            {
                return Failed("Order was not found.", ServiceResponseType.NotFound);
            }

            return new ServiceResponse<GetOrder>(true, "Guest order loaded.", order.Id)
            {
                Payload = Map(order),
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static GetOrder Map(Order order)
        {
            return new GetOrder
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
                    ProductName = line.ProductName,
                    Sku = line.Sku,
                    Image = line.Image,
                    ProductVariantId = line.ProductVariantId,
                    VariantAttributes = ProductVariantAttributeNormalizer.Deserialize(line.VariantAttributesJson),
                }).ToArray(),
            };
        }

        private static ServiceResponse<GetOrder> Failed(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<GetOrder>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string ComputeSha256(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
