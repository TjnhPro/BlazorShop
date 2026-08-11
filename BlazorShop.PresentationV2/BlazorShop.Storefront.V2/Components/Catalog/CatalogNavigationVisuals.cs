using BlazorShop.Storefront.Components.Contracts.Navigation;
using BlazorShop.Storefront.Components.Ssr.Catalog;
using BlazorShop.Storefront.Components.Ssr.Navigation;

namespace BlazorShop.Storefront.V2.Components.Catalog;

internal static class CatalogNavigationVisuals
{
    public static StorefrontPaginationClasses PaginationClasses { get; } = new(
        Root: "mt-8 flex flex-wrap items-center justify-center gap-2",
        Link: "inline-flex min-h-10 min-w-10 items-center justify-center rounded border px-3 text-sm font-semibold",
        CurrentLink: "border-neutral-900 bg-neutral-900 text-white",
        InactiveLink: "border-neutral-200 bg-white text-neutral-900 hover:bg-neutral-100");

    public static StorefrontPaginationLabels CategoryPaginationLabels { get; } = new("Category product pages");

    public static StorefrontPaginationLabels SearchPaginationLabels { get; } = new("Search result pages");

    public static StorefrontCatalogFilterPanelClasses CategoryFilterClasses { get; } = new(
        Root: "mt-6 grid gap-3 rounded-2xl border border-neutral-200 bg-neutral-50/80 p-4 md:grid-cols-3 xl:grid-cols-6",
        Input: "rounded-xl border border-neutral-300 bg-white px-4 py-3 text-sm text-neutral-900 focus:outline-none focus:ring-1 focus:ring-black/10",
        Select: "rounded-xl border border-neutral-300 bg-white px-4 py-3 text-sm text-neutral-900 focus:outline-none focus:ring-1 focus:ring-black/10",
        CheckboxLabel: "flex items-center gap-2 rounded-xl border border-neutral-300 bg-white px-4 py-3 text-sm font-semibold text-neutral-700",
        SubmitButton: "inline-flex items-center justify-center rounded bg-neutral-900 px-4 py-3 text-sm font-semibold text-white hover:bg-neutral-800");

    public static StorefrontCatalogFilterPanelLabels CategoryFilterLabels { get; } = CreateFilterLabels("Apply");

    public static StorefrontCatalogFilterPanelClasses SearchFilterClasses { get; } = new(
        Root: "grid gap-3 md:grid-cols-4 xl:grid-cols-8",
        Input: "rounded border border-neutral-300 bg-white px-4 py-3 text-sm text-neutral-900",
        Select: "rounded border border-neutral-300 bg-white px-3 py-3 text-sm font-semibold text-neutral-900",
        CheckboxLabel: "flex items-center gap-2 rounded-xl border border-neutral-300 bg-white px-4 py-3 text-sm font-semibold text-neutral-700",
        SubmitButton: "inline-flex items-center justify-center gap-2 rounded bg-amber-500 px-4 py-3 text-sm font-semibold text-white hover:bg-amber-600");

    public static StorefrontCatalogFilterPanelLabels SearchFilterLabels { get; } = CreateFilterLabels("Search");

    public static StorefrontBreadcrumbClasses BreadcrumbClasses { get; } = new(
        List: "flex flex-wrap items-center gap-2 text-sm text-neutral-500",
        Item: "flex items-center gap-2",
        Link: "font-medium text-neutral-500 hover:text-neutral-900",
        Current: "font-semibold text-neutral-900",
        Separator: "text-neutral-300");

    public static StorefrontBreadcrumbLabels BreadcrumbLabels { get; } = new();

    private static StorefrontCatalogFilterPanelLabels CreateFilterLabels(string submit)
    {
        return new StorefrontCatalogFilterPanelLabels(
            AllCategories: "All",
            SearchPlaceholder: "Search products",
            MinPricePlaceholder: "Min price",
            MaxPricePlaceholder: "Max price",
            SortAriaLabel: "Sort products",
            CategoryAriaLabel: "Filter by category",
            PageSizeAriaLabel: "Products per page",
            FeaturedSort: "Featured",
            RecentlyUpdatedSort: "Recently updated",
            PriceLowSort: "Price low",
            PriceHighSort: "Price high",
            NewestSort: "Newest",
            InStock: "In stock",
            Submit: submit,
            PerPageSuffix: "per page");
    }
}
