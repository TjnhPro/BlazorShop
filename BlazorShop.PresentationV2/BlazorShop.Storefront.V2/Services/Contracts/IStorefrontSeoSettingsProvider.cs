namespace BlazorShop.Storefront.Services.Contracts
{


    using BlazorShop.Storefront.Models;
public interface IStorefrontSeoSettingsProvider
    {
        Task<SeoSettingsDto?> GetAsync(CancellationToken cancellationToken = default);
    }
}
