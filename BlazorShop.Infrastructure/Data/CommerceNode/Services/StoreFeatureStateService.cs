namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class StoreFeatureStateService : IStoreFeatureStateService
    {
        private static readonly StoreFeatureDefinition[] Definitions =
        [
            new(StoreFeatureKeys.Checkout, "Checkout", "Allow customers to preview checkout and place orders.", true, true),
            new(StoreFeatureKeys.CustomerAccounts, "Customer accounts", "Allow Storefront customer account flows.", true, true),
            new(StoreFeatureKeys.Newsletter, "Newsletter", "Allow Storefront newsletter subscription flows.", true, true),
            new(StoreFeatureKeys.Recommendations, "Recommendations", "Expose product recommendations in Storefront surfaces.", true, true),
            new(StoreFeatureKeys.Reviews, "Reviews", "Reserve review capability state for future review surfaces.", true, false),
        ];

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly IAdminAuditService auditService;
        private readonly IStorefrontPublicConfigurationCache publicConfigurationCache;

        public StoreFeatureStateService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IAdminAuditService auditService,
            IStorefrontPublicConfigurationCache publicConfigurationCache)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.auditService = auditService;
            this.publicConfigurationCache = publicConfigurationCache;
        }

        public async Task<IReadOnlyList<StoreFeatureStateDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return [];
            }

            return await this.GetForStoreAsync(storeResult.Payload, cancellationToken);
        }

        public async Task<ServiceResponse<StoreFeatureStateDto>> UpdateAsync(
            string featureKey,
            UpdateStoreFeatureStateRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var definition = ResolveDefinition(featureKey);
            if (definition is null)
            {
                return Failure("Feature key is not supported.", ServiceResponseType.ValidationError);
            }

            var reason = NormalizeNullable(request.Reason);
            if (reason?.Length > 500)
            {
                return Failure("Feature state reason must be 500 characters or fewer.", ServiceResponseType.ValidationError);
            }

            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return Failure("Current store could not be resolved.", ServiceResponseType.NotFound);
            }

            var state = await this.context.StoreFeatureStates
                .FirstOrDefaultAsync(
                    candidate => candidate.StoreId == storeResult.Payload && candidate.FeatureKey == definition.FeatureKey,
                    cancellationToken);

            if (state is null)
            {
                state = new StoreFeatureState
                {
                    StoreId = storeResult.Payload,
                    FeatureKey = definition.FeatureKey,
                    Enabled = request.Enabled,
                    Reason = reason,
                };
                this.context.StoreFeatureStates.Add(state);
            }
            else
            {
                state.Enabled = request.Enabled;
                state.Reason = reason;
                state.UpdatedAt = DateTime.UtcNow;
            }

            await this.context.SaveChangesAsync(cancellationToken);

            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = "StoreFeatureState.Updated",
                EntityType = "StoreFeatureState",
                EntityId = state.Id.ToString(),
                Summary = $"Feature '{state.FeatureKey}' updated.",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    state.StoreId,
                    state.FeatureKey,
                    state.Enabled,
                    ReasonProvided = state.Reason is not null,
                }),
            });
            await this.publicConfigurationCache.InvalidateAsync(storeResult.Payload, cancellationToken);

            return Success(Map(definition, state), "Feature state updated successfully.");
        }

        public async Task<StoreFeatureStateSnapshot> ResolveAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            var states = await this.context.StoreFeatureStates
                .AsNoTracking()
                .Where(state => state.StoreId == storeId)
                .ToDictionaryAsync(state => state.FeatureKey, StringComparer.Ordinal, cancellationToken);

            return new StoreFeatureStateSnapshot(
                CustomerAccountsEnabled: IsEnabled(states, StoreFeatureKeys.CustomerAccounts),
                CheckoutEnabled: IsEnabled(states, StoreFeatureKeys.Checkout),
                NewsletterEnabled: IsEnabled(states, StoreFeatureKeys.Newsletter),
                RecommendationsEnabled: IsEnabled(states, StoreFeatureKeys.Recommendations),
                ReviewsEnabled: IsEnabled(states, StoreFeatureKeys.Reviews));
        }

        public async Task<bool> IsEnabledAsync(Guid storeId, string featureKey, CancellationToken cancellationToken = default)
        {
            var definition = ResolveDefinition(featureKey);
            if (definition is null)
            {
                return false;
            }

            var enabled = await this.context.StoreFeatureStates
                .AsNoTracking()
                .Where(state => state.StoreId == storeId && state.FeatureKey == definition.FeatureKey)
                .Select(state => (bool?)state.Enabled)
                .FirstOrDefaultAsync(cancellationToken);

            return enabled ?? definition.DefaultEnabled;
        }

        private async Task<IReadOnlyList<StoreFeatureStateDto>> GetForStoreAsync(
            Guid storeId,
            CancellationToken cancellationToken)
        {
            var states = await this.context.StoreFeatureStates
                .AsNoTracking()
                .Where(state => state.StoreId == storeId)
                .ToDictionaryAsync(state => state.FeatureKey, StringComparer.Ordinal, cancellationToken);

            return Definitions
                .Select(definition =>
                {
                    states.TryGetValue(definition.FeatureKey, out var state);
                    return Map(definition, state);
                })
                .ToArray();
        }

        private static StoreFeatureDefinition? ResolveDefinition(string? featureKey)
        {
            var normalized = NormalizeNullable(featureKey);
            return normalized is null
                ? null
                : Definitions.FirstOrDefault(definition => string.Equals(definition.FeatureKey, normalized, StringComparison.Ordinal));
        }

        private static StoreFeatureStateDto Map(StoreFeatureDefinition definition, StoreFeatureState? state)
        {
            return new StoreFeatureStateDto(
                definition.FeatureKey,
                definition.DisplayName,
                definition.Description,
                state?.Enabled ?? definition.DefaultEnabled,
                definition.DefaultEnabled,
                definition.PubliclyVisible,
                state?.Reason,
                state?.UpdatedAt);
        }

        private static bool IsEnabled(
            IReadOnlyDictionary<string, StoreFeatureState> states,
            string featureKey)
        {
            var definition = ResolveDefinition(featureKey)
                ?? throw new InvalidOperationException($"Unknown store feature key '{featureKey}'.");

            return states.TryGetValue(definition.FeatureKey, out var state)
                ? state.Enabled
                : definition.DefaultEnabled;
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static ServiceResponse<StoreFeatureStateDto> Success(StoreFeatureStateDto payload, string message)
        {
            return new ServiceResponse<StoreFeatureStateDto>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<StoreFeatureStateDto> Failure(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<StoreFeatureStateDto>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private sealed record StoreFeatureDefinition(
            string FeatureKey,
            string DisplayName,
            string Description,
            bool DefaultEnabled,
            bool PubliclyVisible);
    }
}
