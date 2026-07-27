namespace BlazorShop.Storefront.Services
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using BlazorShop.Storefront.Runtime;
    using BlazorShop.Storefront.Services.Contracts;

    using GeneratedClients = BlazorShop.Storefront.Client;

    public sealed class GeneratedStorefrontCheckoutClient : IStorefrontCheckoutClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IStorefrontRuntimeCheckoutFacade checkoutFacade;

        public GeneratedStorefrontCheckoutClient(IStorefrontRuntimeCheckoutFacade checkoutFacade)
        {
            this.checkoutFacade = checkoutFacade;
        }

        public async Task<StorefrontSubmitResult<StorefrontCheckoutPreviewResponse>> PreviewCheckoutAsync(
            string cartToken,
            StorefrontCheckoutPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await this.checkoutFacade.PreviewAsync(
                cartToken,
                Project<GeneratedClients.StorefrontCheckoutPreviewRequest>(request),
                cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCheckoutPreviewResponse, StorefrontCheckoutPreviewResponse>(
                result,
                "Unable to preview checkout right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> StartCheckoutAsync(
            string cartToken,
            CancellationToken cancellationToken = default)
        {
            var result = await this.checkoutFacade.StartAsync(cartToken, cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCheckoutSessionResponse, StorefrontCheckoutSessionResponse>(
                result,
                "Unable to start checkout right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> LoadCheckoutAsync(
            string cartToken,
            Guid checkoutSessionId,
            CancellationToken cancellationToken = default)
        {
            var result = await this.checkoutFacade.LoadAsync(cartToken, checkoutSessionId, cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCheckoutSessionResponse, StorefrontCheckoutSessionResponse>(
                result,
                "Unable to load checkout right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> UpdateCheckoutAddressesAsync(
            string cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutAddressStepRequest request,
            CancellationToken cancellationToken = default,
            string? bearerToken = null)
        {
            var result = await this.checkoutFacade.UpdateAddressesAsync(
                cartToken,
                checkoutSessionId,
                Project<GeneratedClients.StorefrontCheckoutAddressStepRequest>(request),
                bearerToken,
                cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCheckoutSessionResponse, StorefrontCheckoutSessionResponse>(
                result,
                "Unable to update checkout address right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> SelectCheckoutShippingMethodAsync(
            string cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutShippingMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await this.checkoutFacade.SelectShippingMethodAsync(
                cartToken,
                checkoutSessionId,
                Project<GeneratedClients.StorefrontCheckoutShippingMethodRequest>(request),
                cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCheckoutSessionResponse, StorefrontCheckoutSessionResponse>(
                result,
                "Unable to update shipping method right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontCheckoutSessionResponse>> SelectCheckoutPaymentMethodAsync(
            string cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutPaymentMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await this.checkoutFacade.SelectPaymentMethodAsync(
                cartToken,
                checkoutSessionId,
                Project<GeneratedClients.StorefrontCheckoutPaymentMethodRequest>(request),
                cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCheckoutSessionResponse, StorefrontCheckoutSessionResponse>(
                result,
                "Unable to update payment method right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontCheckoutReviewResponse>> ReviewCheckoutAsync(
            string cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await this.checkoutFacade.ReviewAsync(
                cartToken,
                checkoutSessionId,
                Project<GeneratedClients.StorefrontCheckoutReviewRequest>(request),
                cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCheckoutReviewResponse, StorefrontCheckoutReviewResponse>(
                result,
                "Unable to review checkout right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontPlaceOrderResponse>> PlaceOrderAsync(
            StorefrontPlaceOrderRequest request,
            string? cartToken = null,
            CancellationToken cancellationToken = default)
        {
            var result = await this.checkoutFacade.PlaceOrderAsync(
                Project<GeneratedClients.StorefrontPlaceOrderRequest>(request),
                cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontPlaceOrderResponse, StorefrontPlaceOrderResponse>(
                result,
                "Unable to place order right now.");
        }

        private static StorefrontSubmitResult<TTarget> MapSubmitResult<TSource, TTarget>(
            StorefrontRuntimeSubmitResult<TSource> result,
            string fallbackMessage)
        {
            return result.Success
                ? StorefrontSubmitResult<TTarget>.Succeeded(
                    result.Value is null ? default : Project<TTarget>(result.Value),
                    "Request completed.")
                : StorefrontSubmitResult<TTarget>.Failed(result.Error?.Message ?? fallbackMessage, result.Error?.Status);
        }

        private static TTarget Project<TTarget>(object source)
        {
            return JsonSerializer.Deserialize<TTarget>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)
                ?? throw new InvalidOperationException($"Could not project generated Storefront DTO to {typeof(TTarget).Name}.");
        }
    }
}
