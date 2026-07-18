namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Web.SharedV2.Models.Discovery;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;
    using BlazorShop.Web.SharedV2.Models.Category;
    using BlazorShop.Web.SharedV2.Models.Pages;
    using BlazorShop.Web.SharedV2.Models.Product;
    using BlazorShop.Web.SharedV2.Models.Seo;

    using Microsoft.Extensions.Options;

    using GetCategoryTreeNode = BlazorShop.Application.DTOs.Category.GetCategoryTreeNode;

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

    public sealed record StorefrontCustomerOrderShippingMethodResponse(
        string? Key,
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
        IReadOnlyList<SelectedAttributeDto> VariantAttributes,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal);

    public sealed record StorefrontCustomerOrderActionFlagsResponse(
        bool CanRetryPayment,
        bool CanReorder,
        bool CanRequestReturn,
        bool HasDownloads);
}
