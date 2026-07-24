namespace BlazorShop.Storefront.Services.Contracts
{
    using BlazorShop.Storefront.Models;
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
