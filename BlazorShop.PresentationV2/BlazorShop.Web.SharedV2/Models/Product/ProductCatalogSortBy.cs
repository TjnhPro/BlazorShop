namespace BlazorShop.Web.SharedV2.Models.Product
{
    public enum ProductCatalogSortBy
    {
        Newest = 0,
        Oldest = 1,
        PriceLowToHigh = 2,
        PriceHighToLow = 3,
        NameAscending = 4,
        NameDescending = 5,
        DisplayOrder = 6,
        Updated = 7,
    }

    public static class ProductCatalogSortByExtensions
    {
        public static string ToApiValue(this ProductCatalogSortBy sortBy)
        {
            return sortBy switch
            {
                ProductCatalogSortBy.Oldest => "oldest",
                ProductCatalogSortBy.PriceLowToHigh => "priceLowToHigh",
                ProductCatalogSortBy.PriceHighToLow => "priceHighToLow",
                ProductCatalogSortBy.NameAscending => "nameAscending",
                ProductCatalogSortBy.NameDescending => "nameDescending",
                ProductCatalogSortBy.DisplayOrder => "displayOrder",
                ProductCatalogSortBy.Updated => "updated",
                _ => "newest",
            };
        }

        public static bool TryParseApiValue(string? value, out ProductCatalogSortBy sortBy)
        {
            sortBy = value?.Trim() switch
            {
                "newest" => ProductCatalogSortBy.Newest,
                "oldest" => ProductCatalogSortBy.Oldest,
                "priceLowToHigh" => ProductCatalogSortBy.PriceLowToHigh,
                "priceHighToLow" => ProductCatalogSortBy.PriceHighToLow,
                "nameAscending" => ProductCatalogSortBy.NameAscending,
                "nameDescending" => ProductCatalogSortBy.NameDescending,
                "displayOrder" => ProductCatalogSortBy.DisplayOrder,
                "updated" => ProductCatalogSortBy.Updated,
                _ => default,
            };

            if (value is not null
                && Enum.TryParse<ProductCatalogSortBy>(value, ignoreCase: true, out var legacySortBy))
            {
                sortBy = legacySortBy;
                return true;
            }

            return value is "newest"
                or "oldest"
                or "priceLowToHigh"
                or "priceHighToLow"
                or "nameAscending"
                or "nameDescending"
                or "displayOrder"
                or "updated";
        }
    }
}
