namespace BlazorShop.Storefront.Services
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

    public partial class StorefrontApiClient
    {
        public Task<StorefrontSubmitResult<StorefrontCartSessionResponse>> CreateOrResumeCartSessionAsync(
            string? cartToken,
            CancellationToken cancellationToken = default)
        {
            return PostAsync<StorefrontCreateCartSessionRequest, StorefrontCartSessionResponse>(
                StorefrontCartSessionRoute,
                new StorefrontCreateCartSessionRequest { CartToken = cartToken },
                "Unable to create cart right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCartResponse>> GetCartAsync(
            string cartToken,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Get,
                StorefrontCartRoute,
                cartToken,
                request: null,
                "Unable to load cart right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCartResponse>> AddCartLineAsync(
            string cartToken,
            StorefrontCartLineCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Post,
                StorefrontCartLinesRoute,
                cartToken,
                request,
                "Unable to add this item to cart right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCartResponse>> UpdateCartLineAsync(
            string cartToken,
            Guid lineId,
            StorefrontCartLineUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Put,
                $"{StorefrontCartLinesRoute}/{lineId:D}",
                cartToken,
                request,
                "Unable to update this cart line right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCartResponse>> RemoveCartLineAsync(
            string cartToken,
            Guid lineId,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Delete,
                $"{StorefrontCartLinesRoute}/{lineId:D}",
                cartToken,
                request: null,
                "Unable to remove this cart line right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCartResponse>> ClearCartAsync(
            string cartToken,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Delete,
                StorefrontCartRoute,
                cartToken,
                request: null,
                "Unable to clear cart right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCartResponse>> RecalculateCartAsync(
            string cartToken,
            StorefrontCartRecalculateRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Post,
                StorefrontCartRecalculateRoute,
                cartToken,
                request,
                "Unable to refresh cart right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCartResponse>> MergeCurrentCustomerCartAsync(
            string cartToken,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCartResponse>(
                HttpMethod.Post,
                StorefrontCartMergeCurrentCustomerRoute,
                cartToken,
                request: null,
                "Unable to merge cart right now.",
                cancellationToken,
                accessToken);
        }
    }
}
