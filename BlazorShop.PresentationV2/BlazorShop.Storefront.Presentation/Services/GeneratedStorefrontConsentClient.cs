namespace BlazorShop.Storefront.Presentation.Services
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using BlazorShop.Storefront.Runtime;
    using BlazorShop.Storefront.Presentation.Contracts;

    using GeneratedClients = BlazorShop.Storefront.Client;

    public sealed class GeneratedStorefrontConsentClient : IStorefrontConsentClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IStorefrontRuntimeConsentFacade consentFacade;

        public GeneratedStorefrontConsentClient(IStorefrontRuntimeConsentFacade consentFacade)
        {
            this.consentFacade = consentFacade;
        }

        public async Task<StorefrontSubmitResult<StorefrontConsentState>> GetConsentAsync(
            string? visitorKey,
            CancellationToken cancellationToken = default)
        {
            var result = await this.consentFacade.GetCurrentAsync(visitorKey, cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontConsentResponse, StorefrontConsentState>(
                result,
                "Unable to load consent state right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontConsentState>> SaveConsentAsync(
            string visitorKey,
            StorefrontConsentSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await this.consentFacade.SaveAsync(
                visitorKey,
                Project<GeneratedClients.StorefrontConsentSaveRequest>(request),
                cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontConsentResponse, StorefrontConsentState>(
                result,
                "Unable to save consent right now.");
        }

        public async Task<StorefrontSubmitResult<StorefrontConsentState>> RevokeConsentAsync(
            string visitorKey,
            CancellationToken cancellationToken = default)
        {
            var result = await this.consentFacade.RevokeAsync(visitorKey, cancellationToken);
            return MapSubmitResult<GeneratedClients.StorefrontConsentResponse, StorefrontConsentState>(
                result,
                "Unable to revoke consent right now.");
        }

        private static StorefrontSubmitResult<TTarget> MapSubmitResult<TSource, TTarget>(
            StorefrontRuntimeSubmitResult<TSource> result,
            string fallbackMessage)
        {
            if (result.Success && result.Value is not null)
            {
                return StorefrontSubmitResult<TTarget>.Succeeded(Project<TTarget>(result.Value), null);
            }

            return StorefrontSubmitResult<TTarget>.Failed(result.Error?.Message ?? fallbackMessage, result.Error?.Status);
        }

        private static TTarget Project<TTarget>(object source)
        {
            return JsonSerializer.Deserialize<TTarget>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)
                ?? throw new InvalidOperationException($"Could not project generated Storefront DTO to {typeof(TTarget).Name}.");
        }
    }
}
