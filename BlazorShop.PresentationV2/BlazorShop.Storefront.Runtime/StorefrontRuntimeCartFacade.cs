namespace BlazorShop.Storefront.Runtime
{
    using BlazorShop.Storefront.Client;

    using GeneratedCartClient = BlazorShop.Storefront.Client.IStorefrontCartClient;

    public interface IStorefrontRuntimeCartFacade
    {
        Task<StorefrontRuntimeSubmitResult<StorefrontCartSessionResponse>> CreateOrResumeSessionAsync(
            string? cartToken,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontCartResponse>> GetCartAsync(
            string? cartToken,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontCartResponse>> AddLineAsync(
            string? cartToken,
            StorefrontCartLineCreateRequest request,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontCartResponse>> UpdateLineAsync(
            string? cartToken,
            Guid lineId,
            StorefrontCartLineUpdateRequest request,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontCartResponse>> RemoveLineAsync(
            string? cartToken,
            Guid lineId,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontCartResponse>> ClearAsync(
            string? cartToken,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontCartValidationResponse>> ValidateAsync(
            string? cartToken,
            StorefrontCartValidateRequest request,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontCartResponse>> RecalculateAsync(
            string? cartToken,
            StorefrontCartRecalculateRequest request,
            CancellationToken cancellationToken = default);

    }

    public sealed class StorefrontRuntimeCartFacade : IStorefrontRuntimeCartFacade
    {
        private readonly IStorefrontRuntimeContext context;
        private readonly GeneratedCartClient cartClient;

        public StorefrontRuntimeCartFacade(
            IStorefrontRuntimeContext context,
            GeneratedCartClient cartClient)
        {
            this.context = context;
            this.cartClient = cartClient;
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCartSessionResponse>> CreateOrResumeSessionAsync(
            string? cartToken,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontCartSessionResponseCommerceNodeApiResponse, StorefrontCartSessionResponse>(
                storeKey => this.cartClient.CreateSessionAsync(
                    storeKey,
                    new StorefrontCreateCartSessionRequest { CartToken = NormalizeToken(cartToken) },
                    cancellationToken),
                "Unable to create cart right now.");
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCartResponse>> GetCartAsync(
            string? cartToken,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontCartResponseCommerceNodeApiResponse, StorefrontCartResponse>(
                storeKey => this.cartClient.GetAsync(NormalizeToken(cartToken), storeKey, cancellationToken),
                "Unable to load cart right now.");
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCartResponse>> AddLineAsync(
            string? cartToken,
            StorefrontCartLineCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontCartResponseCommerceNodeApiResponse, StorefrontCartResponse>(
                storeKey => this.cartClient.AddLineAsync(NormalizeToken(cartToken), storeKey, request, cancellationToken),
                "Unable to add this item to cart right now.");
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCartResponse>> UpdateLineAsync(
            string? cartToken,
            Guid lineId,
            StorefrontCartLineUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            if (lineId == Guid.Empty)
            {
                return Task.FromResult(StorefrontRuntimeSubmitResult<StorefrontCartResponse>.Failed(BadRequest("Cart line is required.")));
            }

            return ExecuteAsync<StorefrontCartResponseCommerceNodeApiResponse, StorefrontCartResponse>(
                storeKey => this.cartClient.UpdateLineAsync(lineId, NormalizeToken(cartToken), storeKey, request, cancellationToken),
                "Unable to update this cart line right now.");
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCartResponse>> RemoveLineAsync(
            string? cartToken,
            Guid lineId,
            CancellationToken cancellationToken = default)
        {
            if (lineId == Guid.Empty)
            {
                return Task.FromResult(StorefrontRuntimeSubmitResult<StorefrontCartResponse>.Failed(BadRequest("Cart line is required.")));
            }

            return ExecuteAsync<StorefrontCartResponseCommerceNodeApiResponse, StorefrontCartResponse>(
                storeKey => this.cartClient.RemoveLineAsync(lineId, NormalizeToken(cartToken), storeKey, cancellationToken),
                "Unable to remove this cart line right now.");
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCartResponse>> ClearAsync(
            string? cartToken,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontCartResponseCommerceNodeApiResponse, StorefrontCartResponse>(
                storeKey => this.cartClient.ClearAsync(NormalizeToken(cartToken), storeKey, cancellationToken),
                "Unable to clear cart right now.");
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCartValidationResponse>> ValidateAsync(
            string? cartToken,
            StorefrontCartValidateRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontCartValidationResponseCommerceNodeApiResponse, StorefrontCartValidationResponse>(
                storeKey => this.cartClient.ValidateAsync(NormalizeToken(cartToken), storeKey, request, cancellationToken),
                "Unable to validate cart right now.");
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontCartResponse>> RecalculateAsync(
            string? cartToken,
            StorefrontCartRecalculateRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontCartResponseCommerceNodeApiResponse, StorefrontCartResponse>(
                storeKey => this.cartClient.RecalculateAsync(NormalizeToken(cartToken), storeKey, request, cancellationToken),
                "Unable to refresh cart right now.");
        }

        private async Task<StorefrontRuntimeSubmitResult<TData>> ExecuteAsync<TEnvelope, TData>(
            Func<string, Task<TEnvelope>> execute,
            string fallbackMessage)
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
            catch (Exception exception) when (exception is not OperationCanceledException || exception is TaskCanceledException)
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
