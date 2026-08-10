namespace BlazorShop.Storefront.Components.Contracts.Product;

public sealed record ProductVariantListLabels(string SectionHeading)
{
    public static ProductVariantListLabels Empty { get; } = new(string.Empty);
}
