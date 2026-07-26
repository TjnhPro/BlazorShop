namespace BlazorShop.Storefront.Presentation.PagePatterns;

public sealed record StorefrontPageDocument(
    string? Title = null,
    string? Description = null,
    string? CanonicalUrl = null,
    bool RobotsIndex = true,
    bool RobotsFollow = true);
