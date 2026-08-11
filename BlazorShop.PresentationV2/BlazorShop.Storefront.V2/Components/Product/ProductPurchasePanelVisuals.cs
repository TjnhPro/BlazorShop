namespace BlazorShop.Storefront.V2.Components.Product;

using BlazorShop.Storefront.Components.Contracts.Product;

public static class ProductPurchasePanelVisuals
{
    public static ProductPurchaseLabels Labels { get; } = new("Add to Cart", "Added", "View Cart", "Free shipping", "Optional", "Add to Cart", "Choose a variant", "Select a variant", "Quantity", "Select {0}");

    public static ProductPurchasePanelClasses Classes { get; } = new(
        Root: "mt-8 rounded-2xl border border-neutral-200 bg-neutral-50/80 p-5",
        Heading: "text-sm font-semibold uppercase tracking-[0.2em] text-neutral-500",
        Message: "mt-2 text-sm leading-6 text-neutral-700",
        BlockedMessage: "mt-3 rounded border border-rose-200 bg-rose-50 px-3 py-2 text-sm font-semibold text-rose-700",
        DeliveryMetadata: "mt-3 flex flex-wrap gap-2 text-xs font-semibold uppercase tracking-[0.14em]",
        FreeShippingBadge: "rounded bg-emerald-50 px-2 py-1 text-emerald-700",
        DeliveryEstimateBadge: "rounded bg-neutral-100 px-2 py-1 text-neutral-700",
        OptionGroups: "mt-5 grid gap-5",
        OptionGroup: "mt-4",
        OptionLegend: "flex items-center justify-between gap-3 text-sm font-semibold text-neutral-900",
        OptionalLabel: "text-xs font-semibold uppercase tracking-[0.14em] text-neutral-500",
        OptionChoices: "mt-2 flex flex-wrap gap-2",
        OptionChoice: "inline-flex cursor-pointer items-center gap-2 rounded border border-neutral-200 bg-white px-3 py-2 text-sm font-semibold text-neutral-800 hover:border-neutral-400",
        OptionInput: "h-4 w-4 accent-neutral-900",
        ColorOptionInput: "sr-only",
        Select: "mt-2 w-full rounded-xl border border-neutral-300 bg-white px-4 py-3 text-neutral-900 focus:outline-none focus:ring-1 focus:ring-black/10",
        Quantity: "mt-4",
        QuantityLabel: "block text-sm font-semibold text-neutral-900",
        QuantityInput: "mt-2 w-28 rounded-xl border border-neutral-300 bg-white px-4 py-3 text-neutral-900 focus:outline-none focus:ring-1 focus:ring-black/10",
        Actions: "mt-4 flex flex-wrap gap-3",
        AddButton: "inline-flex items-center rounded bg-amber-500 px-4 py-2 font-semibold text-white hover:bg-amber-600",
        DisabledAddButton: "inline-flex cursor-not-allowed items-center rounded bg-neutral-300 px-4 py-2 font-semibold text-neutral-600 hover:bg-neutral-300",
        CartLink: "inline-flex items-center rounded bg-neutral-900 px-4 py-2 font-semibold text-white hover:bg-neutral-800",
        Feedback: "mt-3 text-sm font-medium",
        ValidColorSwatch: "h-5 w-5 rounded-full border border-neutral-300",
        MissingColorSwatch: "h-5 w-5 rounded-full border border-neutral-300 bg-neutral-100");
}
