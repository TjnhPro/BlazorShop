namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public sealed class StorefrontOrderItemRequest
    {
        [Required]
        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public sealed record StorefrontCheckoutResultResponse(
        Guid OrderId,
        string Reference,
        string OrderStatus,
        string PaymentStatus,
        string PaymentMethodKey,
        DateTime CreatedOn);

    public sealed class StorefrontGuestOrderLookupRequest
    {
        [Required]
        [StringLength(64, MinimumLength = 3)]
        public string Reference { get; set; } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 32)]
        public string Token { get; set; } = string.Empty;
    }

    public sealed record StorefrontOrderResponse(
        Guid Id,
        string Reference,
        string Status,
        string OrderStatus,
        string PaymentStatus,
        string PaymentMethodKey,
        DateTime? PaymentAt,
        StorefrontOrderPaymentSummaryResponse? PaymentSummary,
        StorefrontOrderStoreSnapshotResponse? StoreSnapshot,
        string? CurrencyCode,
        decimal TotalAmount,
        StorefrontOrderTotalBreakdownResponse? TotalBreakdown,
        string? BaseCurrencyCode,
        decimal? BaseTotalAmount,
        StorefrontOrderTotalBreakdownResponse? BaseTotalBreakdown,
        decimal? ExchangeRate,
        string? ExchangeRateProviderKey,
        string? ExchangeRateSource,
        DateTimeOffset? ExchangeRateEffectiveAtUtc,
        DateTimeOffset? ExchangeRateExpiresAtUtc,
        DateTime CreatedOn,
        string ShippingStatus,
        string? ShippingCarrier,
        string? TrackingNumber,
        string? TrackingUrl,
        DateTime? ShippedOn,
        DateTime? DeliveredOn,
        string? CustomerName,
        string? CustomerEmail,
        StorefrontShippingAddressResponse? BillingAddress,
        StorefrontShippingAddressResponse? ShippingAddressSnapshot,
        StorefrontShippingAddressResponse ShippingAddress,
        StorefrontOrderShippingMethodResponse? ShippingMethod,
        DateTime? CompletedAt,
        DateTime? CancelledAt,
        IReadOnlyList<StorefrontOrderTrackingEventResponse> TrackingEvents,
        IReadOnlyList<StorefrontOrderHistoryEntryResponse> HistoryEntries,
        IReadOnlyList<StorefrontOrderLineResponse> Lines);

    public sealed class StorefrontCustomerOrderListQuery
    {
        [FromQuery(Name = "pageNumber")]
        [Range(1, int.MaxValue)]
        public int PageNumber { get; init; } = 1;

        [FromQuery(Name = "pageSize")]
        [Range(1, StorefrontContractValidation.MaxPageSize)]
        public int PageSize { get; init; } = 10;
    }

    public sealed record StorefrontCustomerOrderListItemResponse(
        string Reference,
        DateTime CreatedOn,
        string OrderStatus,
        string PaymentStatus,
        string ShippingStatus,
        string? CurrencyCode,
        decimal TotalAmount,
        int ItemCount,
        StorefrontCustomerOrderTrackingSummaryResponse TrackingSummary);

    public sealed record StorefrontCustomerOrderTrackingSummaryResponse(
        string? ShippingCarrier,
        string? TrackingNumber,
        string? TrackingUrl,
        DateTime? ShippedOn,
        DateTime? DeliveredOn,
        DateTimeOffset? LastTrackingEventAtUtc);

    public sealed record StorefrontCustomerOrderDetailResponse(
        string Reference,
        string Status,
        string OrderStatus,
        string PaymentStatus,
        string PaymentMethodKey,
        DateTime? PaymentAt,
        StorefrontOrderPaymentSummaryResponse? PaymentSummary,
        StorefrontOrderStoreSnapshotResponse? StoreSnapshot,
        string? CurrencyCode,
        decimal TotalAmount,
        StorefrontOrderTotalBreakdownResponse? TotalBreakdown,
        string? BaseCurrencyCode,
        decimal? BaseTotalAmount,
        StorefrontOrderTotalBreakdownResponse? BaseTotalBreakdown,
        decimal? ExchangeRate,
        string? ExchangeRateProviderKey,
        string? ExchangeRateSource,
        DateTimeOffset? ExchangeRateEffectiveAtUtc,
        DateTimeOffset? ExchangeRateExpiresAtUtc,
        DateTime CreatedOn,
        string ShippingStatus,
        string? ShippingCarrier,
        string? TrackingNumber,
        string? TrackingUrl,
        DateTime? ShippedOn,
        DateTime? DeliveredOn,
        string? CustomerName,
        string? CustomerEmail,
        StorefrontShippingAddressResponse? BillingAddress,
        StorefrontShippingAddressResponse? ShippingAddressSnapshot,
        StorefrontShippingAddressResponse ShippingAddress,
        StorefrontCustomerOrderShippingMethodResponse? ShippingMethod,
        DateTime? CompletedAt,
        DateTime? CancelledAt,
        IReadOnlyList<StorefrontOrderTrackingEventResponse> TrackingEvents,
        IReadOnlyList<StorefrontOrderHistoryEntryResponse> HistoryEntries,
        IReadOnlyList<StorefrontOrderLineResponse> Lines,
        StorefrontCustomerOrderActionFlagsResponse Actions,
        bool ReceiptMode);

    public sealed record StorefrontCustomerOrderShippingMethodResponse(
        string? Key,
        string? MethodCode,
        string? Name,
        decimal? Total,
        string? CurrencyCode,
        string? DeliveryEstimateText);

    public sealed record StorefrontCustomerOrderActionFlagsResponse(
        bool CanRetryPayment,
        bool CanReorder,
        bool CanRequestReturn,
        bool HasDownloads);

    public sealed record StorefrontOrderPaymentSummaryResponse(
        string PaymentStatus,
        string PaymentMethodKey,
        string? AttemptState,
        decimal? Amount,
        string? CurrencyCode,
        DateTime? PaymentAt,
        DateTimeOffset? UpdatedAtUtc);

    public sealed record StorefrontOrderStoreSnapshotResponse(
        Guid? PublicId,
        string? StoreKey,
        string? Name,
        string? BaseUrl,
        string? CompanyName,
        string? CompanyEmail,
        string? CompanyPhone,
        string? CompanyAddress);

    public sealed record StorefrontOrderTotalBreakdownResponse(
        decimal? Subtotal,
        decimal? ShippingTotal,
        decimal? TaxTotal,
        decimal? DiscountTotal,
        decimal? GrandTotal);

    public sealed record StorefrontOrderShippingMethodResponse(
        string? Key,
        string? ProviderSystemName,
        string? MethodCode,
        string? Name,
        decimal? Total,
        string? CurrencyCode,
        string? DeliveryEstimateText);

    public sealed record StorefrontOrderTrackingEventResponse(
        string Status,
        string Message,
        DateTime OccurredAtUtc,
        string? Location,
        string Source);

    public sealed record StorefrontOrderHistoryEntryResponse(
        string EventType,
        string? OldValue,
        string? NewValue,
        string Message,
        DateTimeOffset CreatedAtUtc);

    public sealed record StorefrontOrderLineResponse(
        Guid ProductId,
        string? ProductName,
        string? Sku,
        string? Image,
        Guid? ProductVariantId,
        IReadOnlyList<StorefrontProductVariantAttributeResponse> VariantAttributes,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal);

    public sealed record StorefrontOrderItemHistoryResponse(
        string? ProductName,
        int QuantityOrdered,
        string? CustomerName,
        string? CustomerEmail,
        decimal AmountPaid,
        DateTime DatePurchased,
        string? TrackingNumber,
        string? TrackingUrl,
        string? ShippingStatus);
}
