namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    public sealed class StorefrontConsentService : IStorefrontConsentService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly CommerceNodeDbContext context;
        private readonly StorefrontConsentOptions options;

        public StorefrontConsentService(
            CommerceNodeDbContext context,
            IOptions<StorefrontConsentOptions> options)
        {
            this.context = context;
            this.options = options.Value;
        }

        public async Task<ServiceResponse<StorefrontConsentSnapshot>> GetCurrentAsync(
            Guid storeId,
            string? visitorKey,
            CancellationToken cancellationToken = default)
        {
            if (!this.options.Enabled)
            {
                return Success(CreateEmptySnapshot(bannerRequired: false));
            }

            if (storeId == Guid.Empty)
            {
                return Failure("Storefront store could not be resolved.");
            }

            if (string.IsNullOrWhiteSpace(visitorKey))
            {
                return Success(CreateEmptySnapshot(this.options.BannerRequired));
            }

            var visitorKeyHash = HashVisitorKey(visitorKey);
            var state = await this.context.StorefrontConsentStates
                .AsNoTracking()
                .Where(consent => consent.StoreId == storeId
                                  && consent.VisitorKeyHash == visitorKeyHash
                                  && consent.ConsentVersion == this.ResolveVersion())
                .OrderByDescending(consent => consent.UpdatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            return Success(state is null || state.RevokedAtUtc.HasValue || state.ExpiresAtUtc <= DateTimeOffset.UtcNow
                ? CreateEmptySnapshot(this.options.BannerRequired)
                : ToSnapshot(state, bannerRequired: false));
        }

        public async Task<ServiceResponse<StorefrontConsentSnapshot>> SaveAsync(
            Guid storeId,
            string visitorKey,
            StorefrontConsentSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!this.options.Enabled)
            {
                return Success(CreateEmptySnapshot(bannerRequired: false));
            }

            if (storeId == Guid.Empty || string.IsNullOrWhiteSpace(visitorKey))
            {
                return Failure("Consent visitor context is required.");
            }

            var now = DateTimeOffset.UtcNow;
            var version = this.ResolveVersion();
            var visitorKeyHash = HashVisitorKey(visitorKey);
            var state = await this.context.StorefrontConsentStates
                .FirstOrDefaultAsync(
                    consent => consent.StoreId == storeId
                               && consent.VisitorKeyHash == visitorKeyHash
                               && consent.ConsentVersion == version,
                    cancellationToken);
            var eventType = "updated";

            if (state is null)
            {
                state = new StorefrontConsentState
                {
                    StoreId = storeId,
                    ConsentKey = Guid.NewGuid().ToString("N"),
                    VisitorKeyHash = visitorKeyHash,
                    ConsentVersion = version,
                    CreatedAtUtc = now,
                };
                this.context.StorefrontConsentStates.Add(state);
                eventType = "accepted";
            }

            state.EssentialAccepted = true;
            state.PreferencesAccepted = request.Preferences;
            state.AnalyticsAccepted = request.Analytics;
            state.MarketingAccepted = request.Marketing;
            state.UpdatedAtUtc = now;
            state.RevokedAtUtc = null;
            state.ExpiresAtUtc = now.AddDays(Math.Clamp(this.options.VisitorCookieLifetimeDays, 1, 3650));

            this.context.StorefrontConsentEvents.Add(CreateEvent(storeId, state, eventType));
            await this.context.SaveChangesAsync(cancellationToken);

            return Success(ToSnapshot(state, bannerRequired: false));
        }

        public async Task<ServiceResponse<StorefrontConsentSnapshot>> RevokeAsync(
            Guid storeId,
            string visitorKey,
            CancellationToken cancellationToken = default)
        {
            if (!this.options.Enabled)
            {
                return Success(CreateEmptySnapshot(bannerRequired: false));
            }

            if (storeId == Guid.Empty || string.IsNullOrWhiteSpace(visitorKey))
            {
                return Failure("Consent visitor context is required.");
            }

            var visitorKeyHash = HashVisitorKey(visitorKey);
            var version = this.ResolveVersion();
            var state = await this.context.StorefrontConsentStates
                .FirstOrDefaultAsync(
                    consent => consent.StoreId == storeId
                               && consent.VisitorKeyHash == visitorKeyHash
                               && consent.ConsentVersion == version,
                    cancellationToken);

            if (state is null)
            {
                return Success(CreateEmptySnapshot(this.options.BannerRequired));
            }

            var now = DateTimeOffset.UtcNow;
            state.EssentialAccepted = true;
            state.PreferencesAccepted = false;
            state.AnalyticsAccepted = false;
            state.MarketingAccepted = false;
            state.UpdatedAtUtc = now;
            state.RevokedAtUtc = now;

            this.context.StorefrontConsentEvents.Add(CreateEvent(storeId, state, "revoked"));
            await this.context.SaveChangesAsync(cancellationToken);

            return Success(ToSnapshot(state, this.options.BannerRequired));
        }

        private string ResolveVersion()
        {
            return string.IsNullOrWhiteSpace(this.options.CurrentVersion)
                ? "default"
                : this.options.CurrentVersion.Trim();
        }

        private StorefrontConsentSnapshot CreateEmptySnapshot(bool bannerRequired)
        {
            return new StorefrontConsentSnapshot(
                this.options.Enabled,
                bannerRequired && this.options.Enabled,
                this.ResolveVersion(),
                ConsentKey: null,
                new StorefrontConsentCategories(
                    Essential: true,
                    Preferences: this.options.OptionalCategoriesDefaultEnabled,
                    Analytics: this.options.OptionalCategoriesDefaultEnabled,
                    Marketing: this.options.OptionalCategoriesDefaultEnabled),
                UpdatedAtUtc: null,
                RevokedAtUtc: null,
                ExpiresAtUtc: null);
        }

        private static StorefrontConsentSnapshot ToSnapshot(StorefrontConsentState state, bool bannerRequired)
        {
            return new StorefrontConsentSnapshot(
                Enabled: true,
                BannerRequired: bannerRequired,
                state.ConsentVersion,
                state.ConsentKey,
                new StorefrontConsentCategories(
                    state.EssentialAccepted,
                    state.PreferencesAccepted,
                    state.AnalyticsAccepted,
                    state.MarketingAccepted),
                state.UpdatedAtUtc,
                state.RevokedAtUtc,
                state.ExpiresAtUtc);
        }

        private static StorefrontConsentEvent CreateEvent(Guid storeId, StorefrontConsentState state, string eventType)
        {
            return new StorefrontConsentEvent
            {
                StoreId = storeId,
                ConsentKey = state.ConsentKey,
                EventType = eventType,
                ConsentVersion = state.ConsentVersion,
                CategoriesJson = JsonSerializer.Serialize(
                    new
                    {
                        essential = state.EssentialAccepted,
                        preferences = state.PreferencesAccepted,
                        analytics = state.AnalyticsAccepted,
                        marketing = state.MarketingAccepted,
                    },
                    JsonOptions),
                OccurredAtUtc = DateTimeOffset.UtcNow,
            };
        }

        private static string HashVisitorKey(string visitorKey)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(visitorKey.Trim()));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static ServiceResponse<StorefrontConsentSnapshot> Success(StorefrontConsentSnapshot snapshot)
        {
            return new ServiceResponse<StorefrontConsentSnapshot>(true, "Storefront consent state loaded.")
            {
                Payload = snapshot,
            };
        }

        private static ServiceResponse<StorefrontConsentSnapshot> Failure(string message)
        {
            return new ServiceResponse<StorefrontConsentSnapshot>(false, message)
            {
                ResponseType = ServiceResponseType.ValidationError,
            };
        }
    }
}
