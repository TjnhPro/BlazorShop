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

    public interface IStorefrontCartClient
    {
        Task<StorefrontSubmitResult<StorefrontCartSessionResponse>> CreateOrResumeCartSessionAsync(
                    string? cartToken,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> GetCartAsync(
                    string cartToken,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> AddCartLineAsync(
                    string cartToken,
                    StorefrontCartLineCreateRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> UpdateCartLineAsync(
                    string cartToken,
                    Guid lineId,
                    StorefrontCartLineUpdateRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> RemoveCartLineAsync(
                    string cartToken,
                    Guid lineId,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> ClearCartAsync(
                    string cartToken,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> RecalculateCartAsync(
                    string cartToken,
                    StorefrontCartRecalculateRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCartResponse>> MergeCurrentCustomerCartAsync(
                    string cartToken,
                    string accessToken,
                    CancellationToken cancellationToken = default);
    }
}
