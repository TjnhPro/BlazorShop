namespace BlazorShop.Storefront.Services.Contracts
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Web.SharedV2.Models.Discovery;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;
    using BlazorShop.Web.SharedV2.Models.Category;
    using BlazorShop.Web.SharedV2.Models.Pages;
    using BlazorShop.Web.SharedV2.Models.Product;
    using BlazorShop.Web.SharedV2.Models.Seo;

    using Microsoft.Extensions.Options;

    using GetCategoryTreeNode = BlazorShop.Application.DTOs.Category.GetCategoryTreeNode;
    using BlazorShop.Storefront.Services;

    public interface IStorefrontStoreConfigurationClient
    {
        Task<StorefrontApiResult<StorefrontCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<StorefrontPublicConfiguration>> GetPublicConfigurationAsync(CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>> SetCurrencyPreferenceAsync(
                    StorefrontCurrencyPreferenceRequest request,
                    CancellationToken cancellationToken = default);
    }
}
