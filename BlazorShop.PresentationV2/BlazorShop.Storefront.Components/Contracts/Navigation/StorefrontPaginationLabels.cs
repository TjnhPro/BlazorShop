namespace BlazorShop.Storefront.Components.Contracts.Navigation;

public sealed record StorefrontPaginationLabels(string AriaLabel)
{
    public static StorefrontPaginationLabels Empty { get; } = new(string.Empty);
}
