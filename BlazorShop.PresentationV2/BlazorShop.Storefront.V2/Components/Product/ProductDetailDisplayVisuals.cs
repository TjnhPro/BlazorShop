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

    public static ProductVariantListLabels VariantListLabels { get; } = new(
        SectionHeading: "Available Variants");

    public static ProductVariantListClasses VariantListClasses { get; } = new(
        Root: "mt-8",
        Heading: "text-sm font-semibold uppercase tracking-[0.2em] text-neutral-500",
        List: "mt-4 grid gap-3 sm:grid-cols-2",
        Item: "rounded-2xl border border-neutral-200 bg-neutral-50/80 px-4 py-3 text-sm text-neutral-700",
        Name: "font-semibold text-neutral-900",
        Attribute: "mt-1",
        Details: "mt-2 flex items-center justify-between gap-2",
        Price: "font-semibold text-neutral-900",
        StockAvailable: "text-emerald-700",
        StockUnavailable: "text-rose-700");
}
