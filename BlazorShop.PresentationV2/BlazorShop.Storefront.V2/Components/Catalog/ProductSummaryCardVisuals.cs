namespace BlazorShop.Storefront.V2.Components.Catalog;

using BlazorShop.Storefront.Components.Contracts.Catalog;

internal static class ProductSummaryCardVisuals
{
    public static ProductSummaryLabels Labels { get; } = new(
        FromPrefix: "From",
        PricePrefix: "Price",
        ImageUnavailableText: "Image unavailable",
        ImageUnavailableAltFormat: "Image unavailable for {0}",
        NewBadge: "New",
        VariantsBadge: "Variants",
        OutOfStockBadge: "Out",
        AddToCart: "Add to Cart",
        AddedToCart: "Added",
        ViewProduct: "View Product",
        SelectVariant: "Select variant on the product page before adding.",
        CurrentlyOutOfStock: "Currently out of stock.",
        CurrentlyUnavailable: "Currently unavailable.");

    public static ProductSummaryCardClasses Classes { get; } = new(
        Root: "group relative flex h-full flex-col overflow-hidden rounded-2xl border border-neutral-200/70 bg-white/95 shadow-md transition-all duration-300 hover:-translate-y-0.5 hover:shadow-2xl",
        Body: "p-6",
        Header: "flex items-start justify-between gap-3",
        Category: "text-xs font-semibold uppercase tracking-[0.18em] text-neutral-500 hover:text-neutral-900",
        Title: "mt-2 block text-lg font-extrabold text-neutral-900 transition-colors group-hover:text-neutral-950",
        BadgeGroup: "flex flex-wrap justify-end gap-2 text-[10px] font-semibold uppercase tracking-[0.18em]",
        Badge: "inline-block rounded px-2 py-0.5",
        NewBadge: "bg-green-600/90 text-white",
        VariantsBadge: "bg-amber-100 text-amber-800",
        OutOfStockBadge: "bg-rose-100 text-rose-800",
        Price: "mt-2 text-[15px]",
        ComparePrice: "ml-2 text-sm font-semibold text-neutral-400 line-through",
        ImageLink: "mt-4 block",
        ImageFrame: "relative aspect-[4/3] overflow-hidden rounded-xl bg-neutral-50",
        Image: "absolute inset-0 h-full w-full object-contain transition-transform duration-300 ease-out group-hover:scale-105",
        ImageFallback: "absolute inset-0 flex flex-col items-center justify-center gap-2 bg-neutral-100 px-4 text-center text-sm font-semibold text-neutral-500",
        Description: "mt-4 text-sm leading-6 text-neutral-700",
        Footer: "mt-auto border-t border-neutral-100 px-6 py-4 text-sm",
        ActionGroup: "flex flex-wrap gap-2",
        PrimaryAction: "inline-flex items-center justify-center rounded-md bg-amber-500 px-4 py-2 font-semibold text-white transition hover:bg-amber-600",
        SecondaryAction: "inline-flex items-center justify-center rounded-md bg-neutral-900 px-4 py-2 font-medium text-white hover:bg-neutral-800",
        Status: "mt-3 text-xs font-medium uppercase tracking-[0.16em] text-neutral-600",
        WarningStatus: "text-rose-600");
}
