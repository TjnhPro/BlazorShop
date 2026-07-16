namespace BlazorShop.Application.CommerceNode.Consent
{
    public sealed class StorefrontConsentOptions
    {
        public bool Enabled { get; set; } = true;

        public string CurrentVersion { get; set; } = "2026-07";

        public bool BannerRequired { get; set; } = true;

        public int VisitorCookieLifetimeDays { get; set; } = 180;

        public int EventRetentionDays { get; set; } = 365;

        public bool OptionalCategoriesDefaultEnabled { get; set; }

        public string PolicyPagePath { get; set; } = "/pages/cookies";
    }
}
