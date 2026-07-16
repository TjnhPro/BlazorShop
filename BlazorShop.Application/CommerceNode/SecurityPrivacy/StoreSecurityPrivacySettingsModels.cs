namespace BlazorShop.Application.CommerceNode.SecurityPrivacy
{
    using BlazorShop.Application.CommerceNode.Captcha;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.DTOs;

    public sealed record StoreSecurityPrivacySettingsDto(
        Guid PublicId,
        StoreConsentAdminSettingsDto Consent,
        StoreCaptchaAdminSettingsDto Captcha,
        StorePrivacyRetentionSettingsDto Privacy,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        string? UpdatedByUserId);

    public sealed record StoreConsentAdminSettingsDto(
        bool Enabled,
        string CurrentVersion,
        bool BannerRequired,
        int VisitorCookieLifetimeDays,
        int EventRetentionDays,
        bool OptionalCategoriesDefaultEnabled,
        string PolicyPagePath);

    public sealed record StoreCaptchaAdminSettingsDto(
        bool Enabled,
        string ProviderSystemName,
        string ProviderDisplayName,
        string? PublicSiteKey,
        bool SecretConfigured,
        DateTimeOffset? LastRotatedAt,
        double MinimumScore,
        StoreCaptchaTargetSettingsDto Targets);

    public sealed record StoreCaptchaTargetSettingsDto(
        bool Login,
        bool Registration,
        bool Newsletter,
        bool PasswordRecovery,
        bool Contact,
        bool Review);

    public sealed record StorePrivacyRetentionSettingsDto(
        int RefreshTokenIpRetentionDays,
        int RefreshTokenUserAgentRetentionDays,
        int ConsentEventRetentionDays,
        int CaptchaVerificationLogRetentionDays,
        int NewsletterConsentEvidenceRetentionDays,
        bool AnonymizeIpAfterRetentionWindow);

    public sealed record UpdateStoreSecurityPrivacySettingsRequest(
        StoreConsentAdminSettingsDto Consent,
        UpdateStoreCaptchaAdminSettingsDto Captcha,
        StorePrivacyRetentionSettingsDto Privacy);

    public sealed record UpdateStoreCaptchaAdminSettingsDto(
        bool Enabled,
        string ProviderSystemName,
        string? PublicSiteKey,
        string? SecretReference,
        bool ClearSecret,
        double MinimumScore,
        StoreCaptchaTargetSettingsDto Targets);

    public sealed record StoreSecurityPrivacyRuntimeSettings(
        StorefrontConsentOptions Consent,
        CaptchaOptions Captcha,
        SecurityPrivacyOptions Privacy);

    public interface IStoreSecurityPrivacySettingsService
    {
        Task<ServiceResponse<StoreSecurityPrivacySettingsDto>> GetAsync(
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreSecurityPrivacySettingsDto>> UpdateAsync(
            UpdateStoreSecurityPrivacySettingsRequest request,
            CancellationToken cancellationToken = default);

        Task<StoreSecurityPrivacyRuntimeSettings> ResolveCurrentAsync(
            CancellationToken cancellationToken = default);
    }
}
