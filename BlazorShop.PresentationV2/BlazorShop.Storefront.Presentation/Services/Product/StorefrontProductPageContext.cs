namespace BlazorShop.Storefront.Presentation.Services.Product;

using BlazorShop.Storefront.Components.Contracts.Product;
using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;

public sealed record StorefrontProductPageContext(
    GetProduct Product,
    IReadOnlyList<StorefrontBreadcrumbItem> Breadcrumbs,
    IReadOnlyList<ProductGalleryItem> GalleryItems,
    ProductPurchasePanelModel PurchasePanel,
    IReadOnlyList<GetCatalogProduct> RelatedProducts,
    StorefrontDisplayContext DisplayContext);
