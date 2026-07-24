namespace BlazorShop.Storefront.Services.Contracts
{


    using BlazorShop.Storefront.Models;
public interface IStorefrontSeoComposer
    {
        Task<SeoMetadataDto> ComposeStaticPageAsync(string title, string relativePath, string fallbackMetaDescription, CancellationToken cancellationToken = default);

        Task<SeoMetadataDto> ComposeHomePageAsync(GetStorefrontPage? homePage, string fallbackTitle, string fallbackMetaDescription, CancellationToken cancellationToken = default);

        Task<SeoMetadataDto> ComposeCategoryPageAsync(GetCategory category, CancellationToken cancellationToken = default);

        Task<SeoMetadataDto> ComposeProductPageAsync(GetProduct product, CancellationToken cancellationToken = default);

        Task<SeoMetadataDto> ComposeStorefrontPageAsync(GetStorefrontPage page, CancellationToken cancellationToken = default);

        Task<SeoMetadataDto> ComposeServiceUnavailablePageAsync(string title, string relativePath, string fallbackMetaDescription, CancellationToken cancellationToken = default);

        Task<SeoMetadataDto> ComposeNotFoundPageAsync(string title, string relativePath, string fallbackMetaDescription, CancellationToken cancellationToken = default);
    }
}
