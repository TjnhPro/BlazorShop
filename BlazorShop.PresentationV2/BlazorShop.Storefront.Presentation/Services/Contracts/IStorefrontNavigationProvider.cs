namespace BlazorShop.Storefront.Presentation.Contracts
{
    using BlazorShop.Storefront.Presentation.Models;

    public interface IStorefrontNavigationProvider
    {
        Task<StoreNavigationPublicMenuDto?> GetMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default);
    }
}
