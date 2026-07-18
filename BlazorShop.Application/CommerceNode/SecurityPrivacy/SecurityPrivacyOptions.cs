namespace BlazorShop.Application.CommerceNode.SecurityPrivacy
{
    public sealed class SecurityPrivacyOptions
    {
        public const string SectionName = "Runtime:SecurityPrivacy";

        public int RefreshTokenIpRetentionDays { get; set; } = 30;

        public int RefreshTokenUserAgentRetentionDays { get; set; } = 30;

        public int ConsentEventRetentionDays { get; set; } = 365;

        public int CaptchaVerificationLogRetentionDays { get; set; } = 30;

        public int NewsletterConsentEvidenceRetentionDays { get; set; } = 365;

        public bool AnonymizeIpAfterRetentionWindow { get; set; } = true;

        public string DefaultRegistrationMode { get; set; } = "standard";
    }
}
