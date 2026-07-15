namespace BlazorShop.Storefront.Services.Contracts
{
    using BlazorShop.Application.CommerceNode.StorefrontPages;

    public interface IStorefrontPageNavigationProvider
    {
        Task<IReadOnlyList<StorefrontPageNavigationLinkDto>> GetLinksAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StorefrontPageNavigationLinkDto>> GetLinksByLocationAsync(
            string navigationLocation,
            CancellationToken cancellationToken = default);
    }
}
