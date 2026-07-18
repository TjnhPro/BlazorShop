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

    public interface IStorefrontCustomerClient
    {
        Task<StorefrontSubmitResult<StorefrontCustomerProfileResponse>> GetCustomerProfileAsync(
                    string bearerToken,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCustomerProfileResponse>> UpdateCustomerProfileAsync(
                    string bearerToken,
                    StorefrontCustomerProfileUpdateRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<IReadOnlyList<StorefrontCustomerAddressResponse>>> GetCustomerAddressesAsync(
                    string bearerToken,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> CreateCustomerAddressAsync(
                    string bearerToken,
                    StorefrontCustomerAddressRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> UpdateCustomerAddressAsync(
                    string bearerToken,
                    Guid addressId,
                    StorefrontCustomerAddressRequest request,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<object>> DeleteCustomerAddressAsync(
                    string bearerToken,
                    Guid addressId,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> SetDefaultShippingAddressAsync(
                    string bearerToken,
                    Guid addressId,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> SetDefaultBillingAddressAsync(
                    string bearerToken,
                    Guid addressId,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<PagedResult<StorefrontCustomerOrderListItemResponse>>> GetCustomerOrdersAsync(
                    string bearerToken,
                    int pageNumber = 1,
                    int pageSize = 10,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>> GetCustomerOrderAsync(
                    string bearerToken,
                    string orderReference,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>> GetCustomerOrderReceiptAsync(
                    string bearerToken,
                    string orderReference,
                    CancellationToken cancellationToken = default);
    }
}
