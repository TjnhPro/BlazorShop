namespace BlazorShop.Storefront.Presentation.Contracts
{
    using BlazorShop.Storefront.Presentation.Models;
    using BlazorShop.Storefront.Presentation.Services;

    public interface IStorefrontStructuredDataComposer
    {
        Task<StorefrontStructuredDataDocument> ComposeHomePageAsync(CancellationToken cancellationToken = default);

        Task<StorefrontStructuredDataDocument> ComposeWebPageAsync(string title, string relativePath, string? description, CancellationToken cancellationToken = default);

        Task<StorefrontStructuredDataDocument> ComposeCollectionPageAsync(string title, string relativePath, string? description, CancellationToken cancellationToken = default);

        Task<StorefrontStructuredDataDocument> ComposeFaqPageAsync(string title, string relativePath, string? description, IReadOnlyList<StorefrontFaqStructuredDataItem> faqItems, CancellationToken cancellationToken = default);

        Task<StorefrontStructuredDataDocument> ComposeCategoryPageAsync(GetCategory category, CancellationToken cancellationToken = default);

        Task<StorefrontStructuredDataDocument> ComposeProductPageAsync(GetProduct product, CancellationToken cancellationToken = default);
    }
}
