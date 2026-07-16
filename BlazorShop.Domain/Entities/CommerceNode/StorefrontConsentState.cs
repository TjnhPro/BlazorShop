namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StorefrontConsentState
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string ConsentKey { get; set; } = Guid.NewGuid().ToString("N");

        public string VisitorKeyHash { get; set; } = string.Empty;

        public string ConsentVersion { get; set; } = string.Empty;

        public bool EssentialAccepted { get; set; } = true;

        public bool PreferencesAccepted { get; set; }

        public bool AnalyticsAccepted { get; set; }

        public bool MarketingAccepted { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? RevokedAtUtc { get; set; }

        public DateTimeOffset ExpiresAtUtc { get; set; } = DateTimeOffset.UtcNow.AddDays(180);

        public CommerceStore? Store { get; set; }
    }
}
