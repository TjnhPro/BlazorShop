namespace BlazorShop.Storefront.Presentation.Contracts
{
    using BlazorShop.Storefront.Presentation.Models;

    public interface IStorefrontPageNavigationProvider
    {
        Task<IReadOnlyList<StorefrontPageNavigationLinkDto>> GetLinksAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StorefrontPageNavigationLinkDto>> GetLinksByLocationAsync(
            string navigationLocation,
            CancellationToken cancellationToken = default);
    }
}
