using BlazorShop.Storefront.Components.Contracts.Catalog;

namespace BlazorShop.Storefront.Browser.Catalog;

public interface IStorefrontBrowserProductRailController
{
    Task<StorefrontDiscountedProductRailResponse> GetDiscountedProductRailAsync(
        int limit,
        StorefrontDiscountedProductRailActionDescriptor? actionDescriptor = null,
        CancellationToken cancellationToken = default);
}
