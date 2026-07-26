namespace BlazorShop.Storefront.Runtime
{
    using BlazorShop.Storefront.Client;

    using GeneratedCheckoutClient = BlazorShop.Storefront.Client.IStorefrontCheckoutClient;

    public interface IStorefrontRuntimeCheckoutFacade
    {
        Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutPreviewResponse>> PreviewAsync(
            string? cartToken,
            StorefrontCheckoutPreviewRequest request,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>> StartAsync(
            string? cartToken,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>> LoadAsync(
            string? cartToken,
            Guid checkoutSessionId,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>> UpdateAddressesAsync(
            string? cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutAddressStepRequest request,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>> SelectShippingMethodAsync(
            string? cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutShippingMethodRequest request,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>> SelectPaymentMethodAsync(
            string? cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutPaymentMethodRequest request,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutReviewResponse>> ReviewAsync(
            string? cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutReviewRequest request,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontPlaceOrderResponse>> PlaceOrderAsync(
            StorefrontPlaceOrderRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed class StorefrontRuntimeCheckoutFacade : IStorefrontRuntimeCheckoutFacade
    {
        private readonly IStorefrontRuntimeContext context;
        private readonly GeneratedCheckoutClient checkoutClient;

        public StorefrontRuntimeCheckoutFacade(
            IStorefrontRuntimeContext context,
            GeneratedCheckoutClient checkoutClient)
        {
            this.context = context;
            this.checkoutClient = checkoutClient;
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutPreviewResponse>> PreviewAsync(
            string? cartToken,
            StorefrontCheckoutPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontCheckoutPreviewResponseCommerceNodeApiResponse, StorefrontCheckoutPreviewResponse>(
                storeKey => this.checkoutClient.PreviewAsync(NormalizeToken(cartToken), storeKey, request, cancellationToken),
                "Unable to preview checkout right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>> StartAsync(
            string? cartToken,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontCheckoutSessionResponseCommerceNodeApiResponse, StorefrontCheckoutSessionResponse>(
                storeKey => this.checkoutClient.StartAsync(NormalizeToken(cartToken), storeKey, new StorefrontCheckoutStartRequest(), cancellationToken),
                "Unable to start checkout right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>> LoadAsync(
            string? cartToken,
            Guid checkoutSessionId,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>.Failed(BadRequest("Checkout session is required.")));
            }

            return ExecuteAsync<StorefrontCheckoutSessionResponseCommerceNodeApiResponse, StorefrontCheckoutSessionResponse>(
                storeKey => this.checkoutClient.LoadAsync(checkoutSessionId, NormalizeToken(cartToken), storeKey, cancellationToken),
                "Unable to load checkout right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>> UpdateAddressesAsync(
            string? cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutAddressStepRequest request,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>.Failed(BadRequest("Checkout session is required.")));
            }

            return ExecuteAsync<StorefrontCheckoutSessionResponseCommerceNodeApiResponse, StorefrontCheckoutSessionResponse>(
                storeKey => this.checkoutClient.UpdateAddressesAsync(checkoutSessionId, NormalizeToken(cartToken), storeKey, request, cancellationToken),
                "Unable to update checkout address right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>> SelectShippingMethodAsync(
            string? cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutShippingMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>.Failed(BadRequest("Checkout session is required.")));
            }

            return ExecuteAsync<StorefrontCheckoutSessionResponseCommerceNodeApiResponse, StorefrontCheckoutSessionResponse>(
                storeKey => this.checkoutClient.SelectShippingMethodAsync(checkoutSessionId, NormalizeToken(cartToken), storeKey, request, cancellationToken),
                "Unable to update shipping method right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>> SelectPaymentMethodAsync(
            string? cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutPaymentMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse>.Failed(BadRequest("Checkout session is required.")));
            }

            return ExecuteAsync<StorefrontCheckoutSessionResponseCommerceNodeApiResponse, StorefrontCheckoutSessionResponse>(
                storeKey => this.checkoutClient.SelectPaymentMethodAsync(checkoutSessionId, NormalizeToken(cartToken), storeKey, request, cancellationToken),
                "Unable to update payment method right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCheckoutReviewResponse>> ReviewAsync(
            string? cartToken,
            Guid checkoutSessionId,
            StorefrontCheckoutReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            if (checkoutSessionId == Guid.Empty)
            {
                return Task.FromResult(StorefrontRuntimeSubmitResult<StorefrontCheckoutReviewResponse>.Failed(BadRequest("Checkout session is required.")));
            }

            return ExecuteAsync<StorefrontCheckoutReviewResponseCommerceNodeApiResponse, StorefrontCheckoutReviewResponse>(
                storeKey => this.checkoutClient.ReviewAsync(checkoutSessionId, NormalizeToken(cartToken), storeKey, request, cancellationToken),
                "Unable to review checkout right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontPlaceOrderResponse>> PlaceOrderAsync(
            StorefrontPlaceOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontPlaceOrderResponseCommerceNodeApiResponse, StorefrontPlaceOrderResponse>(
                storeKey => this.checkoutClient.PlaceOrderAsync(storeKey, request, cancellationToken),
                "Unable to place order right now.",
                cancellationToken);
        }

        private async Task<StorefrontRuntimeSubmitResult<TData>> ExecuteAsync<TEnvelope, TData>(
            Func<string, Task<TEnvelope>> execute,
            string fallbackMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await execute(this.context.RequireStoreKey()).ConfigureAwait(false);
                if (response is null)
                {
                    return StorefrontRuntimeSubmitResult<TData>.Failed(ServiceUnavailable(fallbackMessage));
                }

                var success = response.GetType().GetProperty("Success")?.GetValue(response) as bool?;
                var data = response.GetType().GetProperty("Data")?.GetValue(response);
                var message = response.GetType().GetProperty("Message")?.GetValue(response) as string;
                return success == true && data is TData typedData
                    ? StorefrontRuntimeSubmitResult<TData>.Succeeded(typedData)
                    : StorefrontRuntimeSubmitResult<TData>.Failed(ServiceUnavailable(message ?? fallbackMessage));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return StorefrontRuntimeSubmitResult<TData>.Failed(StorefrontRuntimeErrorMapper.FromException(exception));
            }
        }

        private static string? NormalizeToken(string? cartToken)
        {
            return string.IsNullOrWhiteSpace(cartToken) ? null : cartToken.Trim();
        }

        private static StorefrontRuntimeError BadRequest(string message)
        {
            return new StorefrontRuntimeError(400, "request.invalid", message, null, EmptyFieldErrors());
        }

        private static StorefrontRuntimeError ServiceUnavailable(string message)
        {
            return new StorefrontRuntimeError(
                StorefrontRuntimeStatusCodes.ServiceUnavailable,
                "storefront.unavailable",
                message,
                null,
                EmptyFieldErrors());
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyFieldErrors()
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        }
    }
}
