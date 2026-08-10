namespace BlazorShop.Storefront.Components.Contracts.Product;

public sealed record ProductPricingClasses(
    string Root = "",
    string Label = "",
    string PriceRow = "",
    string Price = "",
    string ComparePrice = "",
    string Hidden = "");
