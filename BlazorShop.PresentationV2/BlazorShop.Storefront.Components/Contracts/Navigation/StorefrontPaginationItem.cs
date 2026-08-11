namespace BlazorShop.Storefront.Components.Contracts.Navigation;

public sealed record StorefrontPaginationItem(
    int PageNumber,
    string Href,
    bool IsCurrent,
    string? Label = null);
