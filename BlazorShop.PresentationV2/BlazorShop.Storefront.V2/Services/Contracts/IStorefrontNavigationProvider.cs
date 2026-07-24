namespace BlazorShop.Storefront.Services.Contracts
{


    using BlazorShop.Storefront.Models;
public interface IStorefrontNavigationProvider
    {
        Task<StoreNavigationPublicMenuDto?> GetMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default);
    }
}
