namespace BlazorShop.Storefront.Presentation.Contracts
{


    using BlazorShop.Storefront.Presentation.Models;
public interface IStorefrontSeoSettingsProvider
    {
        Task<SeoSettingsDto?> GetAsync(CancellationToken cancellationToken = default);
    }
}
