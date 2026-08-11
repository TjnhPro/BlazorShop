namespace BlazorShop.Storefront.Components.Ssr.Catalog;

public sealed record StorefrontCatalogFilterPanelLabels(
    string AllCategories,
    string SearchPlaceholder,
    string MinPricePlaceholder,
    string MaxPricePlaceholder,
    string SortAriaLabel,
    string CategoryAriaLabel,
    string PageSizeAriaLabel,
    string FeaturedSort,
    string RecentlyUpdatedSort,
    string PriceLowSort,
    string PriceHighSort,
    string NewestSort,
    string InStock,
    string Submit,
    string PerPageSuffix)
{
    public static StorefrontCatalogFilterPanelLabels Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}
