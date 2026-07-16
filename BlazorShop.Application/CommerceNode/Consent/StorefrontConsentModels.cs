namespace BlazorShop.Application.CommerceNode.Consent
{
    public sealed record StorefrontConsentCategories(
        bool Essential,
        bool Preferences,
        bool Analytics,
        bool Marketing);

    public sealed record StorefrontConsentSnapshot(
        bool Enabled,
        bool BannerRequired,
        string ConsentVersion,
        string? ConsentKey,
        StorefrontConsentCategories Categories,
        DateTimeOffset? UpdatedAtUtc,
        DateTimeOffset? RevokedAtUtc,
        DateTimeOffset? ExpiresAtUtc);

    public sealed record StorefrontConsentSaveRequest(
        bool Preferences,
        bool Analytics,
        bool Marketing);
}
