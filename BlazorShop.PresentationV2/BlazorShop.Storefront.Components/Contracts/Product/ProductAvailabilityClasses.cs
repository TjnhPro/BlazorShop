namespace BlazorShop.Storefront.Components.Contracts.Product;

public sealed record ProductAvailabilityClasses(
    string Root = "",
    string Summary = "",
    string Metadata = "",
    string StockAvailable = "",
    string StockUnavailable = "",
    string Hidden = "");
