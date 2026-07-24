namespace BlazorShop.Storefront.Services
{
    public sealed record StorefrontConsentConfiguration(
        bool Enabled,
        bool BannerRequired,
        string CurrentVersion,
        string PolicyPagePath,
        IReadOnlyList<StorefrontConsentCategory> Categories,
        int VisitorCookieLifetimeDays);

    public sealed record StorefrontConsentCategory(
        string Name,
        bool Required,
        bool DefaultEnabled);

    public sealed record StorefrontConsentState(
        bool Enabled,
        bool BannerRequired,
        string ConsentVersion,
        string? ConsentKey,
        StorefrontConsentCategorySelection Categories,
        DateTimeOffset? UpdatedAtUtc,
        DateTimeOffset? RevokedAtUtc,
        DateTimeOffset? ExpiresAtUtc);

    public sealed record StorefrontConsentCategorySelection(
        bool Essential,
        bool Preferences,
        bool Analytics,
        bool Marketing);

    public sealed class StorefrontConsentSaveRequest
    {
        public bool Preferences { get; set; }

        public bool Analytics { get; set; }

        public bool Marketing { get; set; }
    }
}
