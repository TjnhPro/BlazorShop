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

    public interface IStorefrontCheckoutClient
    {
        Task<StorefrontSubmitResult<StorefrontCheckoutPreviewResponse>> PreviewCheckoutAsync(
                    string cartToken,
                    StorefrontCheckoutPreviewRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> StartCheckoutAsync(
                    string cartToken,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> LoadCheckoutAsync(
                    string cartToken,
                    Guid checkoutSessionId,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> UpdateCheckoutAddressesAsync(
                    string cartToken,
                    Guid checkoutSessionId,
                    StorefrontCheckoutAddressStepRequest request,
                    CancellationToken cancellationToken = default,
                    string? bearerToken = null);

        Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> SelectCheckoutShippingMethodAsync(
                    string cartToken,
                    Guid checkoutSessionId,
                    StorefrontCheckoutShippingMethodRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> SelectCheckoutPaymentMethodAsync(
                    string cartToken,
                    Guid checkoutSessionId,
                    StorefrontCheckoutPaymentMethodRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCheckoutReviewResponse>> ReviewCheckoutAsync(
                    string cartToken,
                    Guid checkoutSessionId,
                    StorefrontCheckoutReviewRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontPlaceOrderResponse>> PlaceOrderAsync(
                    StorefrontPlaceOrderRequest request,
                    string? cartToken = null,
                    CancellationToken cancellationToken = default);
    }
}
