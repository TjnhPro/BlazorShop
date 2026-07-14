namespace BlazorShop.Application.CommerceNode.Checkout
{
    using BlazorShop.Application.DTOs;

    public sealed record StorefrontCheckoutPreviewRequest(
        Guid StoreId,
        string CartToken,
        int ExpectedCartVersion,
        string CustomerEmail,
        string CustomerName,
        string PaymentMethodKey,
        StorefrontCheckoutShippingAddressDto ShippingAddress);

    public sealed record StorefrontCheckoutShippingAddressDto(
        string FullName,
        string Email,
        string? Phone,
        string Address1,
        string? Address2,
        string City,
        string? State,
        string PostalCode,
        string CountryCode);

    public sealed record StorefrontCheckoutPreviewResult(
        Guid CheckoutSessionId,
        Guid CartId,
        int CartVersion,
        string State,
        bool IsValid,
        string NextAction,
        string CustomerEmail,
        string CustomerName,
        string PaymentMethodKey,
        decimal Subtotal,
        decimal ShippingTotal,
        decimal TaxTotal,
        decimal DiscountTotal,
        decimal GrandTotal,
        string CurrencyCode,
        DateTimeOffset ExpiresAtUtc,
        IReadOnlyList<StorefrontCheckoutLineSummary> Lines,
        IReadOnlyList<StorefrontCheckoutValidationIssue> Issues);

    public sealed record StorefrontPlaceOrderRequest(
        Guid StoreId,
        Guid CheckoutSessionId,
        int ExpectedCartVersion,
        string IdempotencyKey);

    public sealed record StorefrontPlaceOrderResult(
        Guid CheckoutSessionId,
        Guid OrderId,
        string Reference,
        string OrderStatus,
        string PaymentStatus,
        string PaymentMethodKey,
        decimal TotalAmount,
        string CurrencyCode,
        string IdempotencyKey,
        DateTime CreatedOn);

    public sealed record StorefrontCheckoutLineSummary(
        Guid LineId,
        Guid ProductId,
        Guid? ProductVariantId,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal,
        string CurrencyCode);

    public sealed record StorefrontCheckoutValidationIssue(
        string Code,
        string Message,
        string? Field = null,
        Guid? LineId = null,
        Guid? ProductId = null);

    public interface IStorefrontCheckoutService
    {
        Task<ServiceResponse<StorefrontCheckoutPreviewResult>> PreviewAsync(
            StorefrontCheckoutPreviewRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontPlaceOrderResult>> PlaceOrderAsync(
            StorefrontPlaceOrderRequest request,
            CancellationToken cancellationToken = default);
    }
}
