namespace BlazorShop.Storefront.Services.Contracts;

using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Services;

public interface IStorefrontSitemapReader
{
    Task<StorefrontApiResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default);
}

public interface IStorefrontSeoSettingsReader
{
    Task<StorefrontApiResult<GetSeoSettings>> GetSeoSettingsAsync(CancellationToken cancellationToken = default);
}
