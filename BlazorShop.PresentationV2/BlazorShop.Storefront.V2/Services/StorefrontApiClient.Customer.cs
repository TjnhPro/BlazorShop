namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Storefront.Models;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;


    public partial class StorefrontApiClient
    {
        public Task<StorefrontSubmitResult<StorefrontCustomerProfileResponse>> GetCustomerProfileAsync(
            string bearerToken,
            CancellationToken cancellationToken = default)
        {
            return SendAuthorizedAsync<StorefrontCustomerProfileResponse>(
                HttpMethod.Get,
                StorefrontCustomerProfileRoute,
                bearerToken,
                request: null,
                "Unable to load customer profile right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCustomerProfileResponse>> UpdateCustomerProfileAsync(
            string bearerToken,
            StorefrontCustomerProfileUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendAuthorizedAsync<StorefrontCustomerProfileResponse>(
                HttpMethod.Put,
                StorefrontCustomerProfileRoute,
                bearerToken,
                request,
                "Unable to update customer profile right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<IReadOnlyList<StorefrontCustomerAddressResponse>>> GetCustomerAddressesAsync(
            string bearerToken,
            CancellationToken cancellationToken = default)
        {
            return SendAuthorizedAsync<IReadOnlyList<StorefrontCustomerAddressResponse>>(
                HttpMethod.Get,
                StorefrontCustomerAddressesRoute,
                bearerToken,
                request: null,
                "Unable to load saved addresses right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> CreateCustomerAddressAsync(
            string bearerToken,
            StorefrontCustomerAddressRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendAuthorizedAsync<StorefrontCustomerAddressResponse>(
                HttpMethod.Post,
                StorefrontCustomerAddressesRoute,
                bearerToken,
                request,
                "Unable to save this address right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> UpdateCustomerAddressAsync(
            string bearerToken,
            Guid addressId,
            StorefrontCustomerAddressRequest request,
            CancellationToken cancellationToken = default)
        {
            if (addressId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCustomerAddressResponse>.Failed("Address is required."));
            }

            return SendAuthorizedAsync<StorefrontCustomerAddressResponse>(
                HttpMethod.Put,
                $"{StorefrontCustomerAddressesRoute}/{addressId:D}",
                bearerToken,
                request,
                "Unable to update this address right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<object>> DeleteCustomerAddressAsync(
            string bearerToken,
            Guid addressId,
            CancellationToken cancellationToken = default)
        {
            if (addressId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<object>.Failed("Address is required."));
            }

            return SendAuthorizedAsync<object>(
                HttpMethod.Delete,
                $"{StorefrontCustomerAddressesRoute}/{addressId:D}",
                bearerToken,
                request: null,
                "Unable to delete this address right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> SetDefaultShippingAddressAsync(
            string bearerToken,
            Guid addressId,
            CancellationToken cancellationToken = default)
        {
            return SetDefaultCustomerAddressAsync(bearerToken, addressId, "default-shipping", cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> SetDefaultBillingAddressAsync(
            string bearerToken,
            Guid addressId,
            CancellationToken cancellationToken = default)
        {
            return SetDefaultCustomerAddressAsync(bearerToken, addressId, "default-billing", cancellationToken);
        }
        private Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> SetDefaultCustomerAddressAsync(
            string bearerToken,
            Guid addressId,
            string command,
            CancellationToken cancellationToken)
        {
            if (addressId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCustomerAddressResponse>.Failed("Address is required."));
            }

            return SendAuthorizedAsync<StorefrontCustomerAddressResponse>(
                HttpMethod.Post,
                $"{StorefrontCustomerAddressesRoute}/{addressId:D}/{command}",
                bearerToken,
                request: null,
                "Unable to update this address right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<PagedResult<StorefrontCustomerOrderListItemResponse>>> GetCustomerOrdersAsync(
            string bearerToken,
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var route = string.Create(
                CultureInfo.InvariantCulture,
                $"{StorefrontCustomerOrdersRoute}?pageNumber={Math.Max(1, pageNumber)}&pageSize={Math.Clamp(pageSize <= 0 ? 10 : pageSize, 1, 100)}");
            return SendAuthorizedAsync<PagedResult<StorefrontCustomerOrderListItemResponse>>(
                HttpMethod.Get,
                route,
                bearerToken,
                request: null,
                "Unable to load orders right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>> GetCustomerOrderAsync(
            string bearerToken,
            string orderReference,
            CancellationToken cancellationToken = default)
        {
            var reference = NormalizeOrderReference(orderReference);
            if (reference is null)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>.Failed("Order reference is required."));
            }

            return SendAuthorizedAsync<StorefrontCustomerOrderDetailResponse>(
                HttpMethod.Get,
                $"{StorefrontCustomerOrdersRoute}/{Uri.EscapeDataString(reference)}",
                bearerToken,
                request: null,
                "Unable to load this order right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>> GetCustomerOrderReceiptAsync(
            string bearerToken,
            string orderReference,
            CancellationToken cancellationToken = default)
        {
            var reference = NormalizeOrderReference(orderReference);
            if (reference is null)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>.Failed("Order reference is required."));
            }

            return SendAuthorizedAsync<StorefrontCustomerOrderDetailResponse>(
                HttpMethod.Get,
                $"{StorefrontCustomerOrdersRoute}/{Uri.EscapeDataString(reference)}/receipt",
                bearerToken,
                request: null,
                "Unable to load this receipt right now.",
                cancellationToken);
        }
    }
}
