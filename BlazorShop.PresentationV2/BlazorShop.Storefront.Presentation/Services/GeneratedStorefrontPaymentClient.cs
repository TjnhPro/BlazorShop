namespace BlazorShop.Storefront.Services
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using BlazorShop.Storefront.Runtime;
    using BlazorShop.Storefront.Services.Contracts;

    using GeneratedClients = BlazorShop.Storefront.Client;

    public sealed class GeneratedStorefrontPaymentClient : IStorefrontPaymentClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IStorefrontRuntimePaymentFacade paymentFacade;

        public GeneratedStorefrontPaymentClient(IStorefrontRuntimePaymentFacade paymentFacade)
        {
            this.paymentFacade = paymentFacade;
        }

        public async Task<StorefrontApiResult<IReadOnlyList<StorefrontPublicPaymentMethod>>> GetPaymentMethodsAsync(
            CancellationToken cancellationToken = default)
        {
            var result = await this.paymentFacade.ListMethodsAsync(cancellationToken);
            if (result.Success && result.Value is not null)
            {
                return StorefrontApiResult<IReadOnlyList<StorefrontPublicPaymentMethod>>.Success(
                    Project<IReadOnlyList<StorefrontPublicPaymentMethod>>(result.Value));
            }

            return result.Error?.Status == StorefrontRuntimeStatusCodes.NotFound
                ? StorefrontApiResult<IReadOnlyList<StorefrontPublicPaymentMethod>>.Success([])
                : StorefrontApiResult<IReadOnlyList<StorefrontPublicPaymentMethod>>.ServiceUnavailable();
        }

        public async Task<StorefrontApiResult<StorefrontPaymentAttemptResponse>> GetPaymentAttemptAsync(
            Guid paymentAttemptId,
            CancellationToken cancellationToken = default)
        {
            var result = await this.paymentFacade.GetAttemptAsync(paymentAttemptId, cancellationToken);
            if (result.Success && result.Value is not null)
            {
                return StorefrontApiResult<StorefrontPaymentAttemptResponse>.Success(
                    Project<StorefrontPaymentAttemptResponse>(result.Value));
            }

            return result.Error?.Status == StorefrontRuntimeStatusCodes.NotFound
                ? StorefrontApiResult<StorefrontPaymentAttemptResponse>.NotFound()
                : StorefrontApiResult<StorefrontPaymentAttemptResponse>.ServiceUnavailable();
        }

        private static TTarget Project<TTarget>(object source)
        {
            return JsonSerializer.Deserialize<TTarget>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)
                ?? throw new InvalidOperationException($"Could not project generated Storefront DTO to {typeof(TTarget).Name}.");
        }
    }
}
