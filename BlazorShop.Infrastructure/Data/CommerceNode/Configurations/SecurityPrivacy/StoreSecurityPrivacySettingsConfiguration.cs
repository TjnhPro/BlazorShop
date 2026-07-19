namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.SecurityPrivacy
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StoreSecurityPrivacySettingsConfiguration : IEntityTypeConfiguration<StoreSecurityPrivacySettings>
    {
        public void Configure(EntityTypeBuilder<StoreSecurityPrivacySettings> entity)
        {
            entity.ToTable("store_security_privacy_settings");
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.Id).HasColumnName("id");
            entity.Property(settings => settings.PublicId).HasColumnName("public_id");
            entity.Property(settings => settings.StoreId).HasColumnName("store_id");
            entity.Property(settings => settings.ConsentEnabled).HasColumnName("consent_enabled").HasDefaultValue(true);
            entity.Property(settings => settings.ConsentVersion).HasColumnName("consent_version").HasMaxLength(64).IsRequired();
            entity.Property(settings => settings.ConsentBannerRequired).HasColumnName("consent_banner_required").HasDefaultValue(true);
            entity.Property(settings => settings.VisitorCookieLifetimeDays).HasColumnName("visitor_cookie_lifetime_days");
            entity.Property(settings => settings.ConsentEventRetentionDays).HasColumnName("consent_event_retention_days");
            entity.Property(settings => settings.OptionalCategoriesDefaultEnabled).HasColumnName("optional_categories_default_enabled");
            entity.Property(settings => settings.PolicyPagePath).HasColumnName("policy_page_path").HasMaxLength(256).IsRequired();
            entity.Property(settings => settings.CaptchaEnabled).HasColumnName("captcha_enabled");
            entity.Property(settings => settings.CaptchaProviderSystemName).HasColumnName("captcha_provider_system_name").HasMaxLength(64).IsRequired();
            entity.Property(settings => settings.CaptchaPublicSiteKey).HasColumnName("captcha_public_site_key").HasMaxLength(256);
            entity.Property(settings => settings.CaptchaSecretReference).HasColumnName("captcha_secret_reference").HasMaxLength(512);
            entity.Property(settings => settings.CaptchaSecretLastRotatedAt).HasColumnName("captcha_secret_last_rotated_at").HasColumnType("timestamp with time zone");
            entity.Property(settings => settings.CaptchaMinimumScore).HasColumnName("captcha_minimum_score");
            entity.Property(settings => settings.CaptchaLoginEnabled).HasColumnName("captcha_login_enabled");
            entity.Property(settings => settings.CaptchaRegistrationEnabled).HasColumnName("captcha_registration_enabled");
            entity.Property(settings => settings.CaptchaNewsletterEnabled).HasColumnName("captcha_newsletter_enabled");
            entity.Property(settings => settings.CaptchaPasswordRecoveryEnabled).HasColumnName("captcha_password_recovery_enabled");
            entity.Property(settings => settings.CaptchaContactEnabled).HasColumnName("captcha_contact_enabled");
            entity.Property(settings => settings.CaptchaReviewEnabled).HasColumnName("captcha_review_enabled");
            entity.Property(settings => settings.RegistrationMode).HasColumnName("registration_mode").HasMaxLength(32).HasDefaultValue("standard").IsRequired();
            entity.Property(settings => settings.RefreshTokenIpRetentionDays).HasColumnName("refresh_token_ip_retention_days");
            entity.Property(settings => settings.RefreshTokenUserAgentRetentionDays).HasColumnName("refresh_token_user_agent_retention_days");
            entity.Property(settings => settings.CaptchaVerificationLogRetentionDays).HasColumnName("captcha_verification_log_retention_days");
            entity.Property(settings => settings.NewsletterConsentEvidenceRetentionDays).HasColumnName("newsletter_consent_evidence_retention_days");
            entity.Property(settings => settings.AnonymizeIpAfterRetentionWindow).HasColumnName("anonymize_ip_after_retention_window").HasDefaultValue(true);
            entity.Property(settings => settings.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(settings => settings.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(settings => settings.UpdatedByUserId).HasColumnName("updated_by_user_id").HasMaxLength(128);

            entity.HasOne(settings => settings.Store)
                .WithMany()
                .HasForeignKey(settings => settings.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(settings => settings.PublicId).IsUnique();
            entity.HasIndex(settings => settings.StoreId).IsUnique();
        }
    }
}
