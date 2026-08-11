namespace BlazorShop.Storefront.Components.Contracts.Checkout;

public sealed record StorefrontCheckoutViewLabels
{
    public static StorefrontCheckoutViewLabels Empty { get; } = new();

    public string StateLabel { get; init; } = string.Empty;
    public string EmptyCartTitle { get; init; } = string.Empty;
    public string ReadySuffix { get; init; } = string.Empty;
    public string Refresh { get; init; } = string.Empty;
    public string Refreshing { get; init; } = string.Empty;
    public string LoadingText { get; init; } = string.Empty;
    public string ErrorFallback { get; init; } = string.Empty;
    public string CartVersion { get; init; } = string.Empty;
    public string CheckoutVersion { get; init; } = string.Empty;
    public string Total { get; init; } = string.Empty;
    public string Shipping { get; init; } = string.Empty;
    public string ShippingNotRequired { get; init; } = string.Empty;
    public string ShippingUnavailable { get; init; } = string.Empty;
    public string Payment { get; init; } = string.Empty;
    public string SelectedShippingOption { get; init; } = string.Empty;
    public string SelectedPaymentOption { get; init; } = string.Empty;
    public string ReviewLatestCheckout { get; init; } = string.Empty;
    public string PlaceOrder { get; init; } = string.Empty;
    public string PlacingOrder { get; init; } = string.Empty;
}
