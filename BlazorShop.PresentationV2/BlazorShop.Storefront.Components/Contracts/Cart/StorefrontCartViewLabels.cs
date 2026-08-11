namespace BlazorShop.Storefront.Components.Contracts.Cart;

public sealed record StorefrontCartViewLabels
{
    public static StorefrontCartViewLabels Empty { get; } = new();

    public string HeaderEyebrow { get; init; } = string.Empty;
    public string Heading { get; init; } = string.Empty;
    public string IntroductoryText { get; init; } = string.Empty;
    public string ItemCountSingular { get; init; } = string.Empty;
    public string ItemCountPlural { get; init; } = string.Empty;
    public string ItemCountSuffix { get; init; } = string.Empty;
    public string EmptyHeading { get; init; } = string.Empty;
    public string EmptyText { get; init; } = string.Empty;
    public string LoadingText { get; init; } = string.Empty;
    public string ErrorFallback { get; init; } = string.Empty;
    public string BrowseProducts { get; init; } = string.Empty;
    public string BackToHome { get; init; } = string.Empty;
    public string FallbackItemText { get; init; } = string.Empty;
    public string UnitPrice { get; init; } = string.Empty;
    public string Quantity { get; init; } = string.Empty;
    public string LineTotal { get; init; } = string.Empty;
    public string ViewProduct { get; init; } = string.Empty;
    public string Remove { get; init; } = string.Empty;
    public string OrderSummary { get; init; } = string.Empty;
    public string ReadyForCheckout { get; init; } = string.Empty;
    public string Items { get; init; } = string.Empty;
    public string Subtotal { get; init; } = string.Empty;
    public string Total { get; init; } = string.Empty;
    public string ContinueToCheckout { get; init; } = string.Empty;
    public string CheckoutHandoffText { get; init; } = string.Empty;
    public string ClearCart { get; init; } = string.Empty;
    public string KeepShopping { get; init; } = string.Empty;
}
