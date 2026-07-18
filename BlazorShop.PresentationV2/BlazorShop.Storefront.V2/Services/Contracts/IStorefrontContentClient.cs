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

    public interface IStorefrontContentClient
    {
        Task<StorefrontApiResult<GetStorefrontPage>> GetPublishedPageBySlugAsync(string slug, CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>> GetPageNavigationLinksAsync(
                    CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<StoreNavigationPublicMenuDto>> GetNavigationMenuAsync(
                    string systemName,
                    CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<GetSeoSettings>> GetSeoSettingsAsync(CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<SeoRedirectResolutionDto>> GetRedirectResolutionAsync(string path, CancellationToken cancellationToken = default);
    }
}
