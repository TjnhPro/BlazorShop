namespace BlazorShop.Storefront.Components.Contracts.Product;

public sealed record ProductVariantListClasses(
    string Root = "",
    string Heading = "",
    string List = "",
    string Item = "",
    string Name = "",
    string Attribute = "",
    string Details = "",
    string Price = "",
    string StockAvailable = "",
    string StockUnavailable = "");
