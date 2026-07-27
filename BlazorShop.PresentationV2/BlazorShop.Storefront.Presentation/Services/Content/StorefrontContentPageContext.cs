namespace BlazorShop.Storefront.Presentation.Services.Content;

using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Services;

public sealed record StorefrontContentPageContext(
    GetStorefrontPage Page,
    StorefrontPagePresentation Presentation,
    IReadOnlyList<StorefrontBreadcrumbItem> Breadcrumbs);
