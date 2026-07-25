namespace BlazorShop.Storefront.Services
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using BlazorShop.Storefront.Runtime;
    using BlazorShop.Storefront.Services.Contracts;

    using GeneratedClients = BlazorShop.Storefront.Client;

    public sealed class GeneratedStorefrontCartClient : IStorefrontCartClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IStorefrontRuntimeCartFacade cartFacade;
        private readonly StorefrontApiClient manualClient;

        public GeneratedStorefrontCartClient(
            IStorefrontRuntimeCartFacade cartFacade,
            StorefrontApiClient manualClient)
        {
            this.cartFacade = cartFacade;
            this.manualClient = manualClient;
        }

        public async Task<StorefrontSubmitResult<StorefrontCartSessionResponse>> CreateOrResumeCartSessionAsync(
            string? cartToken,
            CancellationToken cancellationToken = default)
        {
            var result = await this.cartFacade.CreateOrResumeSessionAsync(cartToken, cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCartSessionResponse, StorefrontCartSessionResponse>(
                result,
                "Unable to create cart right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontCartResponse>> GetCartAsync(
            string cartToken,
            CancellationToken cancellationToken = default)
        {
            var result = await this.cartFacade.GetCartAsync(cartToken, cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCartResponse, StorefrontCartResponse>(
                result,
                "Unable to load cart right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontCartResponse>> AddCartLineAsync(
            string cartToken,
            StorefrontCartLineCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await this.cartFacade.AddLineAsync(
                cartToken,
                Project<GeneratedClients.StorefrontCartLineCreateRequest>(request),
                cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCartResponse, StorefrontCartResponse>(
                result,
                "Unable to add this item to cart right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontCartResponse>> UpdateCartLineAsync(
            string cartToken,
            Guid lineId,
            StorefrontCartLineUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await this.cartFacade.UpdateLineAsync(
                cartToken,
                lineId,
                Project<GeneratedClients.StorefrontCartLineUpdateRequest>(request),
                cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCartResponse, StorefrontCartResponse>(
                result,
                "Unable to update this cart line right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontCartResponse>> RemoveCartLineAsync(
            string cartToken,
            Guid lineId,
            CancellationToken cancellationToken = default)
        {
            var result = await this.cartFacade.RemoveLineAsync(cartToken, lineId, cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCartResponse, StorefrontCartResponse>(
                result,
                "Unable to remove this cart line right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontCartResponse>> ClearCartAsync(
            string cartToken,
            CancellationToken cancellationToken = default)
        {
            var result = await this.cartFacade.ClearAsync(cartToken, cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCartResponse, StorefrontCartResponse>(
                result,
                "Unable to clear cart right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontCartResponse>> RecalculateCartAsync(
            string cartToken,
            StorefrontCartRecalculateRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await this.cartFacade.RecalculateAsync(
                cartToken,
                Project<GeneratedClients.StorefrontCartRecalculateRequest>(request),
                cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontCartResponse, StorefrontCartResponse>(
                result,
                "Unable to refresh cart right now.");
        }

        public Task<StorefrontSubmitResult<StorefrontCartResponse>> MergeCurrentCustomerCartAsync(
            string cartToken,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            // The generated cart client currently has no per-call bearer token parameter for this protected endpoint.
            // Keep this single auth-sensitive cart exception in the V2 host until the account/auth cutover phase.
            return this.manualClient.MergeCurrentCustomerCartAsync(cartToken, accessToken, cancellationToken);
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
