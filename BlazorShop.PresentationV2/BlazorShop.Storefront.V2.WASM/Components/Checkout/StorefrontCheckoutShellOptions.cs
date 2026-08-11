using BlazorShop.Storefront.Components.Contracts.Checkout;

namespace BlazorShop.Storefront.V2.WASM.Components.Checkout;

public static class StorefrontCheckoutShellOptions
{
    public static StorefrontCheckoutViewLabels Labels { get; } = new()
    {
        StateLabel = "Checkout state",
        EmptyCartTitle = "Cart is empty",
        ReadySuffix = "ready",
        Refresh = "Refresh",
        Refreshing = "Refreshing...",
        LoadingText = "Loading checkout...",
        ErrorFallback = "Checkout could not be refreshed.",
        CartVersion = "Cart version",
        CheckoutVersion = "Checkout version",
        Total = "Total",
        Shipping = "Shipping",
        ShippingNotRequired = "Shipping is not required for this cart.",
        ShippingUnavailable = "Shipping is not available yet.",
        Payment = "Payment",
        SelectedShippingOption = "Selected shipping option",
        SelectedPaymentOption = "Selected payment option",
        ReviewLatestCheckout = "Review latest checkout",
        PlaceOrder = "Place order",
        PlacingOrder = "Placing order..."
    };

    public static StorefrontCheckoutViewClasses Classes { get; } = new()
    {
        Shell = "mb-6 rounded border border-neutral-200 bg-white px-5 py-4",
        HeaderLayout = "flex flex-wrap items-start justify-between gap-4",
        Eyebrow = "text-xs font-semibold uppercase text-neutral-500",
        Title = "mt-1 text-lg font-bold text-neutral-950",
        BodyText = "mt-1 text-sm text-neutral-700",
        RefreshButton = "rounded border border-neutral-300 px-3 py-2 text-sm font-semibold text-neutral-800 hover:bg-neutral-50",
        Error = "mt-4 rounded border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-800",
        MetricsGrid = "mt-4 grid gap-4 md:grid-cols-3",
        MetricCard = "rounded border border-neutral-200 p-3",
        MetricValue = "mt-1 font-mono text-sm text-neutral-900",
        IssuePanel = "mt-4 rounded border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900",
        OptionGrid = "mt-4 grid gap-4 lg:grid-cols-2",
        OptionPanel = "rounded border border-neutral-200 p-4",
        OptionList = "mt-3 space-y-2 text-sm",
        OptionLabel = "flex items-start gap-2 rounded border border-neutral-200 p-3",
        PrimaryButton = "mt-4 rounded bg-neutral-900 px-4 py-2 text-sm font-semibold text-white hover:bg-neutral-800 disabled:cursor-wait disabled:bg-neutral-500",
        SecondaryButton = "mt-4 ml-2 rounded bg-amber-500 px-4 py-2 text-sm font-semibold text-white hover:bg-amber-600 disabled:cursor-wait disabled:bg-amber-300"
    };
}
