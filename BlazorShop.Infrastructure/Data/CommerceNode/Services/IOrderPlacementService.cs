namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;

    public interface IOrderPlacementService
    {
        Task<OrderPlacementResult> PlaceAsync(
            OrderPlacementRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed record OrderPlacementRequest(
        Guid StoreId,
        CheckoutSession CheckoutSession,
        PaymentAttempt? PaymentAttempt,
        OrderSnapshotInput Snapshot);

    public sealed record OrderSnapshotInput(
        string OrderStatus,
        string PaymentStatus,
        string PaymentMethodKey,
        DateTimeOffset? PaymentAtUtc,
        string? PaymentMetadataJson,
        string CurrencyCode,
        decimal TotalAmount,
        OrderPlacementCurrencySnapshot CurrencySnapshot,
        string ShippingStatus,
        StorefrontCheckoutShippingOption? ShippingOption);

    public sealed record OrderPlacementCurrencySnapshot(
        string? BaseCurrencyCode,
        decimal? BaseTotalAmount,
        decimal? ExchangeRate,
        string? ExchangeRateProviderKey,
        string? ExchangeRateSource,
        DateTimeOffset? ExchangeRateEffectiveAtUtc,
        DateTimeOffset? ExchangeRateExpiresAtUtc);

    public sealed record OrderPlacementResult(
        bool Success,
        ServiceResponseType ResponseType,
        string Message,
        Order? Order)
    {
        public static OrderPlacementResult Succeeded(Order order)
        {
            return new OrderPlacementResult(true, ServiceResponseType.Success, "Order placed.", order);
        }

        public static OrderPlacementResult Failed(ServiceResponseType responseType, string message)
        {
            return new OrderPlacementResult(false, responseType, message, null);
        }
    }
}
