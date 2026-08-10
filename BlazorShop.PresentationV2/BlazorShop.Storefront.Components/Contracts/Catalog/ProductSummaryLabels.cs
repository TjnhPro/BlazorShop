namespace BlazorShop.Storefront.Components.Contracts.Catalog;

public sealed record ProductSummaryLabels(
    string FromPrefix,
    string PricePrefix,
    string ImageUnavailableText,
    string ImageUnavailableAltFormat,
    string NewBadge,
    string VariantsBadge,
    string OutOfStockBadge,
    string AddToCart,
    string AddedToCart,
    string ViewProduct,
    string SelectVariant,
    string CurrentlyOutOfStock,
    string CurrentlyUnavailable)
{
    public static ProductSummaryLabels Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        "{0}",
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);

    public string FormatImageUnavailableAlt(string productName)
    {
        return string.Format(ImageUnavailableAltFormat, productName);
    }
}
