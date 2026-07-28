namespace BlazorShop.Storefront.Presentation.Services.Product;

using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Components.Contracts.Product;
using BlazorShop.Storefront.Components.Headless.Product;
using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;

public sealed record StorefrontProductPageContext(
    GetProduct Product,
    IReadOnlyList<StorefrontBreadcrumbItem> Breadcrumbs,
    IReadOnlyList<ProductGalleryItem> GalleryItems,
    ProductPurchasePanelModel PurchasePanel,
    ProductPurchaseActionDescriptor PurchaseActions,
    StorefrontProductPricingView Pricing,
    StorefrontProductAvailabilityView Availability,
    StorefrontProductPurchaseView Purchase,
    IReadOnlyList<StorefrontProductVariantView> Variants,
    StorefrontProductBadgeView Badges,
    StorefrontProductNavigationView Navigation,
    IReadOnlyList<ProductSummaryItem> RelatedProductSummaries,
    StorefrontDisplayContext DisplayContext,
    StorefrontLinkContext Links);

public sealed record StorefrontProductPricingView(
    string PrimaryPriceLabel,
    string PriceDisplay,
    string? ComparePriceDisplay,
    string CurrencyCode);

public sealed record StorefrontProductAvailabilityView(
    string AvailabilityState,
    string AvailabilityLabel,
    string StockLabel,
    string VariantSummary);

public sealed record StorefrontProductPurchaseView(
    bool CanAddToCart,
    string PurchaseMessage,
    string PurchaseBlockMessage,
    string DefaultSkuLabel,
    string DefaultGtinLabel,
    int MinQuantity,
    int? MaxQuantity,
    int InitialStockValue);

public sealed record StorefrontProductVariantView(
    Guid Id,
    string DisplayName,
    string AttributeText,
    string PriceDisplay,
    string StockLabel,
    string AvailabilityState,
    bool IsDefault);

public sealed record StorefrontProductBadgeView(bool IsFreshArrival);

public sealed record StorefrontProductNavigationView(
    string? CategoryName,
    string? CategoryUrl,
    string ProductDescription,
    string ProductSeoContentTitle);
