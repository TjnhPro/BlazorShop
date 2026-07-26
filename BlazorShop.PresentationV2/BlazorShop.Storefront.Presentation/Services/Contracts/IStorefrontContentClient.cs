namespace BlazorShop.Storefront.Services.Contracts
{
    using BlazorShop.Storefront.Models;
    using BlazorShop.Storefront.Services;

    public interface IStorefrontContentClient
    {
        Task<StorefrontApiResult<GetStorefrontPage>> GetPublishedPageBySlugAsync(string slug, CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>> GetPageNavigationLinksAsync(
            CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<StoreNavigationPublicMenuDto>> GetNavigationMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<GetSeoSettings>> GetSeoSettingsAsync(CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<SeoRedirectResolutionDto>> GetRedirectResolutionAsync(string path, CancellationToken cancellationToken = default);
    }
}
