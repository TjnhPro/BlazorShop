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

    public sealed record StorefrontConsentContext(
        bool VisualEnabled,
        string PolicyPagePath,
        StorefrontConsentActionContext Actions,
        StorefrontConsentBrowserEvents Events)
    {
        public static StorefrontConsentContext Default { get; } = Create(null);

        public static StorefrontConsentContext Create(StorefrontConsentConfiguration? configuration)
        {
            var policyPagePath = string.IsNullOrWhiteSpace(configuration?.PolicyPagePath)
                ? "/pages/cookies"
                : configuration.PolicyPagePath.Trim();

            return new StorefrontConsentContext(
                configuration?.Enabled ?? true,
                policyPagePath,
                StorefrontConsentActionContext.Default,
                StorefrontConsentBrowserEvents.Default);
        }
    }

    public sealed record StorefrontConsentActionContext(
        string CurrentUrl,
        string AcceptUrl,
        string RevokeUrl,
        string CurrentMethod,
        string AcceptMethod,
        string RevokeMethod)
    {
        public static StorefrontConsentActionContext Default { get; } = new(
            "/api/consent/current",
            "/api/consent",
            "/api/consent/revoke",
            "GET",
            "POST",
            "POST");
    }

    public sealed record StorefrontConsentBrowserEvents(
        string Changed,
        string ManageRequested)
    {
        public static StorefrontConsentBrowserEvents Default { get; } = new(
            "storefront:consent:changed",
            "storefront:consent:manage-requested");
    }

    public sealed class StorefrontConsentSaveRequest
    {
        public bool Preferences { get; set; }

        public bool Analytics { get; set; }

        public bool Marketing { get; set; }
    }
}
