namespace BlazorShop.Storefront.Services.Contracts
{
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Web.SharedV2.Models.Category;
    using BlazorShop.Web.SharedV2.Models.Pages;
    using BlazorShop.Web.SharedV2.Models.Product;

    public interface IStorefrontSeoComposer
    {
        Task<SeoMetadataDto> ComposeStaticPageAsync(string title, string relativePath, string fallbackMetaDescription, CancellationToken cancellationToken = default);

        Task<SeoMetadataDto> ComposeCategoryPageAsync(GetCategory category, CancellationToken cancellationToken = default);

        Task<SeoMetadataDto> ComposeProductPageAsync(GetProduct product, CancellationToken cancellationToken = default);

        Task<SeoMetadataDto> ComposeStorefrontPageAsync(GetStorefrontPage page, CancellationToken cancellationToken = default);

        Task<SeoMetadataDto> ComposeServiceUnavailablePageAsync(string title, string relativePath, string fallbackMetaDescription, CancellationToken cancellationToken = default);

        Task<SeoMetadataDto> ComposeNotFoundPageAsync(string title, string relativePath, string fallbackMetaDescription, CancellationToken cancellationToken = default);
    }
}
