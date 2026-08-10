namespace BlazorShop.Storefront.V2.Components.Product;

using BlazorShop.Storefront.Components.Contracts.Product;

public static class ProductDetailDisplayVisuals
{
    public static ProductAvailabilityClasses AvailabilityClasses { get; } = new(
        Root: "mt-4 flex flex-wrap items-center gap-3 text-sm text-neutral-600",
        Summary: "inline-flex items-center rounded-full bg-neutral-100 px-3 py-1 font-semibold text-neutral-700",
        Metadata: "inline-flex items-center rounded-full bg-neutral-100 px-3 py-1 font-semibold text-neutral-700",
        StockAvailable: "inline-flex items-center rounded-full bg-emerald-50 px-3 py-1 font-semibold text-emerald-700",
        StockUnavailable: "inline-flex items-center rounded-full bg-rose-50 px-3 py-1 font-semibold text-rose-700",
        Hidden: "hidden");
}
