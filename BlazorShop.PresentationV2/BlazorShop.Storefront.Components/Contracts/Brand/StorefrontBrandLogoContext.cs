namespace BlazorShop.Storefront.Components.Contracts.Brand;

public sealed record StorefrontBrandLogoContext(
    string HomeUrl,
    string BrandName,
    string? BrandLabel = null,
    string? LogoUrl = null,
    string? HomeLabel = null);
