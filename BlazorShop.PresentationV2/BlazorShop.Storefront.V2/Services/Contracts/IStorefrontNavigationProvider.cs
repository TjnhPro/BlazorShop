namespace BlazorShop.Storefront.Services.Contracts
{
    using BlazorShop.Application.CommerceNode.Navigation;

    public interface IStorefrontNavigationProvider
    {
        Task<StoreNavigationPublicMenuDto?> GetMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default);
    }
}
