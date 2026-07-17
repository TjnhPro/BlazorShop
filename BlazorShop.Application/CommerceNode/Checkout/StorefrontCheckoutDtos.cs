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
        StorefrontCheckoutShippingAddressDto? ShippingAddress,
        Guid? ShippingAddressId = null,
        Guid? BillingAddressId = null,
        bool UseShippingAddressAsBillingAddress = true,
        string? CustomerAppUserId = null);

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
        int CheckoutVersion,
        int CartVersion,
        int LastValidatedCartVersion,
        string State,
        string CurrentStep,
        IReadOnlyList<string> CompletedSteps,
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

    public sealed record StorefrontCheckoutStartRequest(
        Guid StoreId,
        string CartToken);

    public sealed record StorefrontCheckoutSessionRequest(
        Guid StoreId,
        Guid CheckoutSessionId,
        string CartToken);

    public sealed record StorefrontCheckoutAddressStepRequest(
        Guid StoreId,
        Guid CheckoutSessionId,
        string CartToken,
        StorefrontCheckoutShippingAddressDto? BillingAddress,
        StorefrontCheckoutShippingAddressDto? ShippingAddress,
        Guid? BillingAddressId = null,
        Guid? ShippingAddressId = null,
        bool UseBillingAddressAsShippingAddress = false,
        string? CustomerAppUserId = null);

    public sealed record StorefrontCheckoutSessionResult(
        Guid CheckoutSessionId,
        Guid CartId,
        int CheckoutVersion,
        int CartVersion,
        int LastValidatedCartVersion,
        string State,
        string CurrentStep,
        IReadOnlyList<string> CompletedSteps,
        bool IsActive,
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
        Guid PaymentAttemptId,
        Guid? OrderId,
        string? Reference,
        string? OrderStatus,
        string PaymentStatus,
        string PaymentMethodKey,
        decimal TotalAmount,
        string CurrencyCode,
        string IdempotencyKey,
        DateTime CreatedOn,
        string? NextActionType = null,
        string? NextActionUrl = null);

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
        Task<ServiceResponse<StorefrontCheckoutSessionResult>> StartAsync(
            StorefrontCheckoutStartRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCheckoutSessionResult>> LoadAsync(
            StorefrontCheckoutSessionRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCheckoutSessionResult>> CancelAsync(
            StorefrontCheckoutSessionRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCheckoutSessionResult>> ExpireAsync(
            StorefrontCheckoutSessionRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCheckoutSessionResult>> UpdateAddressesAsync(
            StorefrontCheckoutAddressStepRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCheckoutPreviewResult>> PreviewAsync(
            StorefrontCheckoutPreviewRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontPlaceOrderResult>> PlaceOrderAsync(
            StorefrontPlaceOrderRequest request,
            CancellationToken cancellationToken = default);
    }
}
