namespace BlazorShop.Storefront.Presentation.Contracts
{
    using BlazorShop.Storefront.Presentation.Services;

    public interface IStorefrontSitemapService
    {
        Task<StorefrontSitemapGenerationResult> GenerateAsync(CancellationToken cancellationToken = default);
    }
}
