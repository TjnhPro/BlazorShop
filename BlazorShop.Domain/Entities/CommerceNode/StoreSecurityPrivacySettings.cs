namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StoreSecurityPrivacySettings
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public CommerceStore? Store { get; set; }

        public bool ConsentEnabled { get; set; } = true;

        public string ConsentVersion { get; set; } = "2026-07";

        public bool ConsentBannerRequired { get; set; } = true;

        public int VisitorCookieLifetimeDays { get; set; } = 180;

        public int ConsentEventRetentionDays { get; set; } = 365;

        public bool OptionalCategoriesDefaultEnabled { get; set; }

        public string PolicyPagePath { get; set; } = "/pages/cookies";

        public bool CaptchaEnabled { get; set; }

        public string CaptchaProviderSystemName { get; set; } = "none";

        public string? CaptchaPublicSiteKey { get; set; }

        public string? CaptchaSecretReference { get; set; }

        public DateTimeOffset? CaptchaSecretLastRotatedAt { get; set; }

        public double CaptchaMinimumScore { get; set; } = 0.5;

        public bool CaptchaLoginEnabled { get; set; }

        public bool CaptchaRegistrationEnabled { get; set; }

        public bool CaptchaNewsletterEnabled { get; set; }

        public bool CaptchaPasswordRecoveryEnabled { get; set; }

        public bool CaptchaContactEnabled { get; set; }

        public bool CaptchaReviewEnabled { get; set; }

        public int RefreshTokenIpRetentionDays { get; set; } = 30;

        public int RefreshTokenUserAgentRetentionDays { get; set; } = 30;

        public int CaptchaVerificationLogRetentionDays { get; set; } = 30;

        public int NewsletterConsentEvidenceRetentionDays { get; set; } = 365;

        public bool AnonymizeIpAfterRetentionWindow { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string? UpdatedByUserId { get; set; }
    }
}
