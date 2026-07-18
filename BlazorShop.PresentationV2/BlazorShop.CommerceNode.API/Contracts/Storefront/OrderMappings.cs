namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.IdentityModel.Tokens.Jwt;

    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Captcha;
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Domain.Contracts;
    public static partial class StorefrontContractMappings
    {
        public static BlazorShop.Application.CommerceNode.Orders.StorefrontGuestOrderLookupRequest ToApplicationRequest(
            this StorefrontGuestOrderLookupRequest request)
        {
            return new BlazorShop.Application.CommerceNode.Orders.StorefrontGuestOrderLookupRequest(
                request.Reference,
                request.Token);
        }
        public static CreateOrderItem ToCreateOrderItem(this StorefrontOrderItemRequest request)
        {
            return new CreateOrderItem
            {
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                SelectedAttributes = request.SelectedAttributes,
                Quantity = request.Quantity,
            };
        }
        public static StorefrontCheckoutResultResponse ToStorefrontContract(this StorefrontCheckoutResult result)
        {
            return new StorefrontCheckoutResultResponse(
                result.OrderId,
                result.Reference,
                result.OrderStatus,
                result.PaymentStatus,
                result.PaymentMethodKey,
                result.CreatedOn);
        }
        public static StorefrontOrderResponse ToStorefrontContract(this GetOrder order)
        {
            return new StorefrontOrderResponse(
                order.Id,
                order.Reference,
                order.Status,
                order.OrderStatus,
                order.PaymentStatus,
                order.PaymentMethodKey,
                order.PaymentAt,
                order.PaymentSummary?.ToStorefrontContract(),
                order.StoreSnapshot?.ToStorefrontContract(),
                order.CurrencyCode,
                order.TotalAmount,
                order.TotalBreakdown?.ToStorefrontContract(),
                order.BaseCurrencyCode,
                order.BaseTotalAmount,
                order.BaseTotalBreakdown?.ToStorefrontContract(),
                order.ExchangeRate,
                order.ExchangeRateProviderKey,
                order.ExchangeRateSource,
                order.ExchangeRateEffectiveAtUtc,
                order.ExchangeRateExpiresAtUtc,
                order.CreatedOn,
                order.ShippingStatus,
                order.ShippingCarrier,
                order.TrackingNumber,
                order.TrackingUrl,
                order.ShippedOn,
                order.DeliveredOn,
                order.CustomerName,
                order.CustomerEmail,
                order.BillingAddress?.ToStorefrontContract(),
                order.ShippingAddressSnapshot?.ToStorefrontContract(),
                new StorefrontShippingAddressResponse(
                    order.ShippingFullName,
                    order.ShippingEmail,
                    order.ShippingPhone,
                    order.ShippingAddress1,
                    order.ShippingAddress2,
                    order.ShippingCity,
                    order.ShippingState,
                    order.ShippingPostalCode,
                    order.ShippingCountryCode),
                order.ShippingMethod?.ToStorefrontContract(),
                order.CompletedAt,
                order.CancelledAt,
                order.TrackingEvents.Select(item => item.ToStorefrontContract()).ToArray(),
                order.HistoryEntries
                    .Where(item => item.VisibleToCustomer)
                    .Select(item => item.ToStorefrontContract())
                    .ToArray(),
                order.Lines.Select(line => line.ToStorefrontContract()).ToArray());
        }
        public static StorefrontCustomerOrderListItemResponse ToCustomerOrderListItemContract(this GetOrder order)
        {
            var lines = order.Lines.ToArray();
            return new StorefrontCustomerOrderListItemResponse(
                order.Reference,
                order.CreatedOn,
                order.OrderStatus,
                order.PaymentStatus,
                order.ShippingStatus,
                order.CurrencyCode,
                order.TotalAmount,
                lines.Sum(item => item.Quantity),
                new StorefrontCustomerOrderTrackingSummaryResponse(
                    order.ShippingCarrier,
                    order.TrackingNumber,
                    order.TrackingUrl,
                    order.ShippedOn,
                    order.DeliveredOn,
                    order.TrackingEvents
                        .OrderByDescending(item => item.OccurredAtUtc)
                        .Select(item => (DateTimeOffset?)item.OccurredAtUtc)
                        .FirstOrDefault()));
        }
        public static StorefrontCustomerOrderDetailResponse ToCustomerOrderDetailContract(
            this GetOrder order,
            bool receiptMode)
        {
            return new StorefrontCustomerOrderDetailResponse(
                order.Reference,
                order.Status,
                order.OrderStatus,
                order.PaymentStatus,
                order.PaymentMethodKey,
                order.PaymentAt,
                order.PaymentSummary?.ToStorefrontContract(),
                order.StoreSnapshot?.ToStorefrontContract(),
                order.CurrencyCode,
                order.TotalAmount,
                order.TotalBreakdown?.ToStorefrontContract(),
                order.BaseCurrencyCode,
                order.BaseTotalAmount,
                order.BaseTotalBreakdown?.ToStorefrontContract(),
                order.ExchangeRate,
                order.ExchangeRateProviderKey,
                order.ExchangeRateSource,
                order.ExchangeRateEffectiveAtUtc,
                order.ExchangeRateExpiresAtUtc,
                order.CreatedOn,
                order.ShippingStatus,
                order.ShippingCarrier,
                order.TrackingNumber,
                order.TrackingUrl,
                order.ShippedOn,
                order.DeliveredOn,
                order.CustomerName,
                order.CustomerEmail,
                order.BillingAddress?.ToStorefrontContract(),
                order.ShippingAddressSnapshot?.ToStorefrontContract(),
                new StorefrontShippingAddressResponse(
                    order.ShippingFullName,
                    order.ShippingEmail,
                    order.ShippingPhone,
                    order.ShippingAddress1,
                    order.ShippingAddress2,
                    order.ShippingCity,
                    order.ShippingState,
                    order.ShippingPostalCode,
                    order.ShippingCountryCode),
                order.ShippingMethod?.ToCustomerOrderContract(),
                order.CompletedAt,
                order.CancelledAt,
                order.TrackingEvents.Select(item => item.ToStorefrontContract()).ToArray(),
                order.HistoryEntries
                    .Where(item => item.VisibleToCustomer)
                    .Select(item => item.ToStorefrontContract())
                    .ToArray(),
                order.Lines.Select(line => line.ToStorefrontContract()).ToArray(),
                new StorefrontCustomerOrderActionFlagsResponse(
                    CanRetryPayment: false,
                    CanReorder: false,
                    CanRequestReturn: false,
                    HasDownloads: false),
                receiptMode);
        }
        public static StorefrontOrderPaymentSummaryResponse ToStorefrontContract(this GetOrderPaymentSummary summary)
        {
            return new StorefrontOrderPaymentSummaryResponse(
                summary.PaymentStatus,
                summary.PaymentMethodKey,
                summary.AttemptState,
                summary.Amount,
                summary.CurrencyCode,
                summary.PaymentAt,
                summary.UpdatedAtUtc);
        }
        public static StorefrontOrderStoreSnapshotResponse ToStorefrontContract(this GetOrderStoreSnapshot snapshot)
        {
            return new StorefrontOrderStoreSnapshotResponse(
                snapshot.PublicId,
                snapshot.StoreKey,
                snapshot.Name,
                snapshot.BaseUrl,
                snapshot.CompanyName,
                snapshot.CompanyEmail,
                snapshot.CompanyPhone,
                snapshot.CompanyAddress);
        }
        public static StorefrontOrderTotalBreakdownResponse ToStorefrontContract(this GetOrderTotalBreakdown breakdown)
        {
            return new StorefrontOrderTotalBreakdownResponse(
                breakdown.Subtotal,
                breakdown.ShippingTotal,
                breakdown.TaxTotal,
                breakdown.DiscountTotal,
                breakdown.GrandTotal);
        }
        public static StorefrontShippingAddressResponse ToStorefrontContract(this GetOrderAddress address)
        {
            return new StorefrontShippingAddressResponse(
                address.FullName,
                address.Email,
                address.Phone,
                address.Address1,
                address.Address2,
                address.City,
                address.State,
                address.PostalCode,
                address.CountryCode);
        }
        public static StorefrontOrderShippingMethodResponse ToStorefrontContract(this GetOrderShippingMethodSnapshot shippingMethod)
        {
            return new StorefrontOrderShippingMethodResponse(
                shippingMethod.Key,
                shippingMethod.ProviderSystemName,
                shippingMethod.MethodCode,
                shippingMethod.Name,
                shippingMethod.Total,
                shippingMethod.CurrencyCode,
                shippingMethod.DeliveryEstimateText);
        }
        public static StorefrontCustomerOrderShippingMethodResponse ToCustomerOrderContract(
            this GetOrderShippingMethodSnapshot shippingMethod)
        {
            return new StorefrontCustomerOrderShippingMethodResponse(
                shippingMethod.Key,
                shippingMethod.MethodCode,
                shippingMethod.Name,
                shippingMethod.Total,
                shippingMethod.CurrencyCode,
                shippingMethod.DeliveryEstimateText);
        }
        public static StorefrontOrderTrackingEventResponse ToStorefrontContract(this GetShipmentTrackingEvent item)
        {
            return new StorefrontOrderTrackingEventResponse(
                item.Status,
                item.Message,
                item.OccurredAtUtc,
                item.Location,
                item.Source);
        }
        public static StorefrontOrderHistoryEntryResponse ToStorefrontContract(this GetOrderHistoryEntry item)
        {
            return new StorefrontOrderHistoryEntryResponse(
                item.EventType,
                item.OldValue,
                item.NewValue,
                item.Message,
                item.CreatedAtUtc);
        }
        public static StorefrontOrderLineResponse ToStorefrontContract(this GetOrderLine line)
        {
            return new StorefrontOrderLineResponse(
                line.ProductId,
                line.ProductName,
                line.Sku,
                line.Image,
                line.ProductVariantId,
                line.VariantAttributes.Select(attribute => attribute.ToStorefrontContract()).ToArray(),
                line.Quantity,
                line.UnitPrice,
                line.LineTotal);
        }
        public static StorefrontOrderItemHistoryResponse ToStorefrontContract(this GetOrderItem item)
        {
            return new StorefrontOrderItemHistoryResponse(
                item.ProductName,
                item.QuantityOrdered,
                item.CustomerName,
                item.CustomerEmail,
                item.AmountPayed,
                item.DatePurchased,
                item.TrackingNumber,
                item.TrackingUrl,
                item.ShippingStatus);
        }
    }
}
