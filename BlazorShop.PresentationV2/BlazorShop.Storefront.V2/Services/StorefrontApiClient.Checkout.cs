namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;


    public partial class StorefrontApiClient
    {
        public Task<StorefrontSubmitResult<StorefrontCheckoutPreviewResponse>> PreviewCheckoutAsync(
            string cartToken,
            StorefrontCheckoutPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCheckoutPreviewResponse>(
                HttpMethod.Post,
                StorefrontCheckoutPreviewRoute,
                cartToken,
                request,
                "Unable to preview checkout right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> StartCheckoutAsync(
            string cartToken,
            CancellationToken cancellationToken = default)
        {
            return SendCartAsync<StorefrontCheckoutSessionResponse>(
                HttpMethod.Post,
                StorefrontCheckoutStartRoute,
                cartToken,
                new StorefrontCheckoutStartRequest(),
                "Unable to start checkout right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> LoadCheckoutAsync(
            string cartToken,
            Guid checkoutSessionId,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCheckoutSessionResponse>.Failed("Checkout session is required."));
            }

            return SendCartAsync<StorefrontCheckoutSessionResponse>(
                HttpMethod.Get,
                $"checkout/{checkoutSessionId:D}",
                cartToken,
                request: null,
                "Unable to load checkout right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> UpdateCheckoutAddressesAsync(
            string cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutAddressStepRequest request,
            CancellationToken cancellationToken = default,
            string? bearerToken = null)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCheckoutSessionResponse>.Failed("Checkout session is required."));
            }

            return SendCartAsync<StorefrontCheckoutSessionResponse>(
                HttpMethod.Post,
                $"checkout/{checkoutSessionId:D}/addresses",
                cartToken,
                request,
                "Unable to update checkout address right now.",
                cancellationToken,
                bearerToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> SelectCheckoutShippingMethodAsync(
            string cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutShippingMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCheckoutSessionResponse>.Failed("Checkout session is required."));
            }

            return SendCartAsync<StorefrontCheckoutSessionResponse>(
                HttpMethod.Post,
                $"checkout/{checkoutSessionId:D}/shipping-method",
                cartToken,
                request,
                "Unable to update shipping method right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> SelectCheckoutPaymentMethodAsync(
            string cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutPaymentMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCheckoutSessionResponse>.Failed("Checkout session is required."));
            }

            return SendCartAsync<StorefrontCheckoutSessionResponse>(
                HttpMethod.Post,
                $"checkout/{checkoutSessionId:D}/payment-method",
                cartToken,
                request,
                "Unable to update payment method right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontCheckoutReviewResponse>> ReviewCheckoutAsync(
            string cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCheckoutReviewResponse>.Failed("Checkout session is required."));
            }

            return SendCartAsync<StorefrontCheckoutReviewResponse>(
                HttpMethod.Post,
                $"checkout/{checkoutSessionId:D}/review",
                cartToken,
                request,
                "Unable to review checkout right now.",
                cancellationToken);
        }
        public Task<StorefrontSubmitResult<StorefrontPlaceOrderResponse>> PlaceOrderAsync(
            StorefrontPlaceOrderRequest request,
            string? cartToken = null,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(cartToken))
            {
                return SendCartAsync<StorefrontPlaceOrderResponse>(
                    HttpMethod.Post,
                    StorefrontPlaceOrderRoute,
                    cartToken,
                    request,
                    "Unable to place order right now.",
                    cancellationToken);
            }

            return PostAsync<StorefrontPlaceOrderRequest, StorefrontPlaceOrderResponse>(
                StorefrontPlaceOrderRoute,
                request,
                "Unable to place order right now.",
                cancellationToken);
        }
    }
}
