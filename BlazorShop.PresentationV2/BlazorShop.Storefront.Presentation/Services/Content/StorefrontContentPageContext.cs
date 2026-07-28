namespace BlazorShop.Storefront.Presentation.Services.Content;

using BlazorShop.Storefront.Presentation.Models;
using BlazorShop.Storefront.Presentation.Services;

public sealed record StorefrontContentPageContext(
    GetStorefrontPage Page,
    StorefrontPagePresentation Presentation,
    IReadOnlyList<StorefrontBreadcrumbItem> Breadcrumbs);
