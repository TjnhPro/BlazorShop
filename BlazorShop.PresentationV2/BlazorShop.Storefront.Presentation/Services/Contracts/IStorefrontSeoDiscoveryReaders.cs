namespace BlazorShop.Storefront.Presentation.Contracts;

using BlazorShop.Storefront.Presentation.Models;
using BlazorShop.Storefront.Presentation.Services;

public interface IStorefrontSitemapReader
{
    Task<StorefrontApiResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default);
}

public interface IStorefrontSeoSettingsReader
{
    Task<StorefrontApiResult<GetSeoSettings>> GetSeoSettingsAsync(CancellationToken cancellationToken = default);
}
