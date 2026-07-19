namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using BlazorShop.Application.CommerceNode.Captcha;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    public sealed class StoreSecurityPrivacySettingsService : IStoreSecurityPrivacySettingsService
    {
        private static readonly Regex ProviderRegex = new("^[a-z0-9._-]{1,64}$", RegexOptions.Compiled);
        private const string RegistrationModeStandard = "standard";
        private const string RegistrationModeDisabled = "disabled";

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly StorefrontConsentOptions consentDefaults;
        private readonly CaptchaOptions captchaDefaults;
        private readonly SecurityPrivacyOptions privacyDefaults;
        private readonly IStorefrontPublicConfigurationCache publicConfigurationCache;
        private readonly ICommerceNodeAuditActorAccessor actorAccessor;
        private readonly IAdminAuditService auditService;

        public StoreSecurityPrivacySettingsService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IOptions<StorefrontConsentOptions> consentDefaults,
            IOptions<CaptchaOptions> captchaDefaults,
            IOptions<SecurityPrivacyOptions> privacyDefaults,
            IStorefrontPublicConfigurationCache publicConfigurationCache,
            ICommerceNodeAuditActorAccessor actorAccessor,
            IAdminAuditService auditService)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.consentDefaults = consentDefaults.Value;
            this.captchaDefaults = captchaDefaults.Value;
            this.privacyDefaults = privacyDefaults.Value;
            this.publicConfigurationCache = publicConfigurationCache;
            this.actorAccessor = actorAccessor;
            this.auditService = auditService;
        }

        public async Task<ServiceResponse<StoreSecurityPrivacySettingsDto>> GetAsync(
            CancellationToken cancellationToken = default)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return Failure(storeResult.Message ?? "Store could not be resolved.");
            }

            var settings = await this.LoadAsync(storeResult.Value, cancellationToken);
            return Success(this.Map(settings, storeResult.Value), "Security and privacy settings loaded.");
        }

        public async Task<ServiceResponse<StoreSecurityPrivacySettingsDto>> UpdateAsync(
            UpdateStoreSecurityPrivacySettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validation = Validate(request);
            if (validation is not null)
            {
                return Failure(validation, ServiceResponseType.ValidationError);
            }

            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return Failure(storeResult.Message ?? "Store could not be resolved.");
            }

            var now = DateTimeOffset.UtcNow;
            var settings = await this.LoadAsync(storeResult.Value, cancellationToken);
            if (settings is null)
            {
                settings = CreateDefaultEntity(storeResult.Value, now);
                this.context.StoreSecurityPrivacySettings.Add(settings);
            }

            Apply(settings, request, now, this.actorAccessor.GetCurrentActor().ActorUserId);
            await this.context.SaveChangesAsync(cancellationToken);
            await this.publicConfigurationCache.InvalidateAsync(storeResult.Payload, cancellationToken);
            await this.LogAsync(settings, cancellationToken);

            return Success(this.Map(settings, storeResult.Payload), "Security and privacy settings updated.");
        }

        public async Task<StoreSecurityPrivacyRuntimeSettings> ResolveCurrentAsync(
            CancellationToken cancellationToken = default)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return this.BuildRuntime(null);
            }

            var settings = await this.LoadAsync(storeResult.Payload, cancellationToken);
            return this.BuildRuntime(settings);
        }

        private async Task<StoreSecurityPrivacySettings?> LoadAsync(Guid storeId, CancellationToken cancellationToken)
        {
            return await this.context.StoreSecurityPrivacySettings
                .AsTracking()
                .FirstOrDefaultAsync(settings => settings.StoreId == storeId, cancellationToken);
        }

        private StoreSecurityPrivacySettings CreateDefaultEntity(Guid storeId, DateTimeOffset now)
        {
            var runtime = this.BuildRuntime(null);
            return new StoreSecurityPrivacySettings
            {
                StoreId = storeId,
                PublicId = Guid.NewGuid(),
                ConsentEnabled = runtime.Consent.Enabled,
                ConsentVersion = runtime.Consent.CurrentVersion,
                ConsentBannerRequired = runtime.Consent.BannerRequired,
                VisitorCookieLifetimeDays = runtime.Consent.VisitorCookieLifetimeDays,
                ConsentEventRetentionDays = runtime.Consent.EventRetentionDays,
                OptionalCategoriesDefaultEnabled = runtime.Consent.OptionalCategoriesDefaultEnabled,
                PolicyPagePath = runtime.Consent.PolicyPagePath,
                CaptchaEnabled = runtime.Captcha.Enabled,
                CaptchaProviderSystemName = runtime.Captcha.ProviderSystemName,
                CaptchaPublicSiteKey = runtime.Captcha.PublicSiteKey,
                CaptchaSecretReference = runtime.Captcha.SecretReference,
                CaptchaMinimumScore = runtime.Captcha.MinimumScore,
                CaptchaLoginEnabled = runtime.Captcha.Targets.Login,
                CaptchaRegistrationEnabled = runtime.Captcha.Targets.Registration,
                CaptchaNewsletterEnabled = runtime.Captcha.Targets.Newsletter,
                CaptchaPasswordRecoveryEnabled = runtime.Captcha.Targets.PasswordRecovery,
                CaptchaContactEnabled = runtime.Captcha.Targets.Contact,
                CaptchaReviewEnabled = runtime.Captcha.Targets.Review,
                RegistrationMode = runtime.Registration.Mode,
                RefreshTokenIpRetentionDays = runtime.Privacy.RefreshTokenIpRetentionDays,
                RefreshTokenUserAgentRetentionDays = runtime.Privacy.RefreshTokenUserAgentRetentionDays,
                CaptchaVerificationLogRetentionDays = runtime.Privacy.CaptchaVerificationLogRetentionDays,
                NewsletterConsentEvidenceRetentionDays = runtime.Privacy.NewsletterConsentEvidenceRetentionDays,
                AnonymizeIpAfterRetentionWindow = runtime.Privacy.AnonymizeIpAfterRetentionWindow,
                CreatedAt = now,
                UpdatedAt = now,
            };
        }

        private StoreSecurityPrivacyRuntimeSettings BuildRuntime(StoreSecurityPrivacySettings? settings)
        {
            if (settings is null)
            {
                return new StoreSecurityPrivacyRuntimeSettings(
                    new StorefrontConsentOptions
                    {
                        Enabled = this.consentDefaults.Enabled,
                        CurrentVersion = this.consentDefaults.CurrentVersion,
                        BannerRequired = this.consentDefaults.BannerRequired,
                        VisitorCookieLifetimeDays = this.consentDefaults.VisitorCookieLifetimeDays,
                        EventRetentionDays = this.consentDefaults.EventRetentionDays,
                        OptionalCategoriesDefaultEnabled = this.consentDefaults.OptionalCategoriesDefaultEnabled,
                        PolicyPagePath = this.consentDefaults.PolicyPagePath,
                    },
                    new CaptchaOptions
                    {
                        Enabled = this.captchaDefaults.Enabled,
                        ProviderSystemName = this.captchaDefaults.ProviderSystemName,
                        PublicSiteKey = this.captchaDefaults.PublicSiteKey,
                        SecretReference = this.captchaDefaults.SecretReference,
                        MinimumScore = this.captchaDefaults.MinimumScore,
                        Targets = new CaptchaTargetOptions
                        {
                            Login = this.captchaDefaults.Targets.Login,
                            Registration = this.captchaDefaults.Targets.Registration,
                            Newsletter = this.captchaDefaults.Targets.Newsletter,
                            PasswordRecovery = this.captchaDefaults.Targets.PasswordRecovery,
                            Contact = this.captchaDefaults.Targets.Contact,
                            Review = this.captchaDefaults.Targets.Review,
                        },
                    },
                    new StoreRegistrationRuntimeSettings(
                        NormalizeRegistrationMode(this.privacyDefaults.DefaultRegistrationMode),
                        string.Equals(
                            NormalizeRegistrationMode(this.privacyDefaults.DefaultRegistrationMode),
                            RegistrationModeStandard,
                            StringComparison.Ordinal)),
                    new SecurityPrivacyOptions
                    {
                        RefreshTokenIpRetentionDays = this.privacyDefaults.RefreshTokenIpRetentionDays,
                        RefreshTokenUserAgentRetentionDays = this.privacyDefaults.RefreshTokenUserAgentRetentionDays,
                        ConsentEventRetentionDays = this.privacyDefaults.ConsentEventRetentionDays,
                        CaptchaVerificationLogRetentionDays = this.privacyDefaults.CaptchaVerificationLogRetentionDays,
                        NewsletterConsentEvidenceRetentionDays = this.privacyDefaults.NewsletterConsentEvidenceRetentionDays,
                        AnonymizeIpAfterRetentionWindow = this.privacyDefaults.AnonymizeIpAfterRetentionWindow,
                        DefaultRegistrationMode = NormalizeRegistrationMode(this.privacyDefaults.DefaultRegistrationMode),
                    });
            }

            var registrationMode = NormalizeRegistrationMode(settings.RegistrationMode);
            return new StoreSecurityPrivacyRuntimeSettings(
                new StorefrontConsentOptions
                {
                    Enabled = settings.ConsentEnabled,
                    CurrentVersion = settings.ConsentVersion,
                    BannerRequired = settings.ConsentBannerRequired,
                    VisitorCookieLifetimeDays = settings.VisitorCookieLifetimeDays,
                    EventRetentionDays = settings.ConsentEventRetentionDays,
                    OptionalCategoriesDefaultEnabled = settings.OptionalCategoriesDefaultEnabled,
                    PolicyPagePath = settings.PolicyPagePath,
                },
                new CaptchaOptions
                {
                    Enabled = settings.CaptchaEnabled,
                    ProviderSystemName = settings.CaptchaProviderSystemName,
                    PublicSiteKey = settings.CaptchaPublicSiteKey,
                    SecretReference = settings.CaptchaSecretReference,
                    MinimumScore = settings.CaptchaMinimumScore,
                    Targets = new CaptchaTargetOptions
                    {
                        Login = settings.CaptchaLoginEnabled,
                        Registration = settings.CaptchaRegistrationEnabled,
                        Newsletter = settings.CaptchaNewsletterEnabled,
                        PasswordRecovery = settings.CaptchaPasswordRecoveryEnabled,
                        Contact = settings.CaptchaContactEnabled,
                        Review = settings.CaptchaReviewEnabled,
                    },
                },
                new StoreRegistrationRuntimeSettings(
                    registrationMode,
                    string.Equals(registrationMode, RegistrationModeStandard, StringComparison.Ordinal)),
                new SecurityPrivacyOptions
                {
                    RefreshTokenIpRetentionDays = settings.RefreshTokenIpRetentionDays,
                    RefreshTokenUserAgentRetentionDays = settings.RefreshTokenUserAgentRetentionDays,
                    ConsentEventRetentionDays = settings.ConsentEventRetentionDays,
                    CaptchaVerificationLogRetentionDays = settings.CaptchaVerificationLogRetentionDays,
                    NewsletterConsentEvidenceRetentionDays = settings.NewsletterConsentEvidenceRetentionDays,
                    AnonymizeIpAfterRetentionWindow = settings.AnonymizeIpAfterRetentionWindow,
                    DefaultRegistrationMode = registrationMode,
                });
        }

        private StoreSecurityPrivacySettingsDto Map(StoreSecurityPrivacySettings? settings, Guid storeId)
        {
            var runtime = this.BuildRuntime(settings);
            var now = DateTimeOffset.UtcNow;
            return new StoreSecurityPrivacySettingsDto(
                settings?.PublicId ?? Guid.Empty,
                new StoreConsentAdminSettingsDto(
                    runtime.Consent.Enabled,
                    runtime.Consent.CurrentVersion,
                    runtime.Consent.BannerRequired,
                    runtime.Consent.VisitorCookieLifetimeDays,
                    runtime.Consent.EventRetentionDays,
                    runtime.Consent.OptionalCategoriesDefaultEnabled,
                    runtime.Consent.PolicyPagePath),
                new StoreCaptchaAdminSettingsDto(
                    runtime.Captcha.Enabled,
                    runtime.Captcha.ProviderSystemName,
                    GetProviderDisplayName(runtime.Captcha.ProviderSystemName),
                    runtime.Captcha.Enabled ? runtime.Captcha.PublicSiteKey : null,
                    !string.IsNullOrWhiteSpace(runtime.Captcha.SecretReference),
                    settings?.CaptchaSecretLastRotatedAt,
                    runtime.Captcha.MinimumScore,
                    new StoreCaptchaTargetSettingsDto(
                        runtime.Captcha.Targets.Login,
                        runtime.Captcha.Targets.Registration,
                        runtime.Captcha.Targets.Newsletter,
                        runtime.Captcha.Targets.PasswordRecovery,
                        runtime.Captcha.Targets.Contact,
                        runtime.Captcha.Targets.Review)),
                new StoreRegistrationAdminSettingsDto(
                    runtime.Registration.Mode,
                    runtime.Registration.RegistrationAllowed),
                new StorePrivacyRetentionSettingsDto(
                    runtime.Privacy.RefreshTokenIpRetentionDays,
                    runtime.Privacy.RefreshTokenUserAgentRetentionDays,
                    runtime.Privacy.ConsentEventRetentionDays,
                    runtime.Privacy.CaptchaVerificationLogRetentionDays,
                    runtime.Privacy.NewsletterConsentEvidenceRetentionDays,
                    runtime.Privacy.AnonymizeIpAfterRetentionWindow),
                settings?.CreatedAt ?? now,
                settings?.UpdatedAt ?? now,
                settings?.UpdatedByUserId);
        }

        private static void Apply(StoreSecurityPrivacySettings settings, UpdateStoreSecurityPrivacySettingsRequest request, DateTimeOffset now, string? actorUserId)
        {
            settings.ConsentEnabled = request.Consent.Enabled;
            settings.ConsentVersion = request.Consent.CurrentVersion.Trim();
            settings.ConsentBannerRequired = request.Consent.BannerRequired;
            settings.VisitorCookieLifetimeDays = request.Consent.VisitorCookieLifetimeDays;
            settings.ConsentEventRetentionDays = request.Consent.EventRetentionDays;
            settings.OptionalCategoriesDefaultEnabled = request.Consent.OptionalCategoriesDefaultEnabled;
            settings.PolicyPagePath = request.Consent.PolicyPagePath.Trim();
            settings.CaptchaEnabled = request.Captcha.Enabled;
            settings.CaptchaProviderSystemName = request.Captcha.ProviderSystemName.Trim().ToLowerInvariant();
            settings.CaptchaPublicSiteKey = NormalizeNullable(request.Captcha.PublicSiteKey);
            settings.CaptchaMinimumScore = request.Captcha.MinimumScore;
            settings.CaptchaLoginEnabled = request.Captcha.Targets.Login;
            settings.CaptchaRegistrationEnabled = request.Captcha.Targets.Registration;
            settings.CaptchaNewsletterEnabled = request.Captcha.Targets.Newsletter;
            settings.CaptchaPasswordRecoveryEnabled = request.Captcha.Targets.PasswordRecovery;
            settings.CaptchaContactEnabled = request.Captcha.Targets.Contact;
            settings.CaptchaReviewEnabled = request.Captcha.Targets.Review;
            settings.RegistrationMode = NormalizeRegistrationMode(request.Registration?.Mode);
            settings.RefreshTokenIpRetentionDays = request.Privacy.RefreshTokenIpRetentionDays;
            settings.RefreshTokenUserAgentRetentionDays = request.Privacy.RefreshTokenUserAgentRetentionDays;
            settings.CaptchaVerificationLogRetentionDays = request.Privacy.CaptchaVerificationLogRetentionDays;
            settings.NewsletterConsentEvidenceRetentionDays = request.Privacy.NewsletterConsentEvidenceRetentionDays;
            settings.AnonymizeIpAfterRetentionWindow = request.Privacy.AnonymizeIpAfterRetentionWindow;
            settings.UpdatedAt = now;
            settings.UpdatedByUserId = actorUserId;

            if (request.Captcha.ClearSecret)
            {
                settings.CaptchaSecretReference = null;
                settings.CaptchaSecretLastRotatedAt = now;
            }
            else if (!string.IsNullOrWhiteSpace(request.Captcha.SecretReference))
            {
                settings.CaptchaSecretReference = request.Captcha.SecretReference.Trim();
                settings.CaptchaSecretLastRotatedAt = now;
            }
        }

        private async Task LogAsync(StoreSecurityPrivacySettings settings, CancellationToken cancellationToken)
        {
            var metadata = JsonSerializer.Serialize(new
            {
                settings.StoreId,
                settings.ConsentEnabled,
                settings.ConsentVersion,
                settings.CaptchaEnabled,
                settings.CaptchaProviderSystemName,
                CaptchaSecretConfigured = !string.IsNullOrWhiteSpace(settings.CaptchaSecretReference),
                settings.RegistrationMode,
                settings.RefreshTokenIpRetentionDays,
                settings.RefreshTokenUserAgentRetentionDays,
            });

            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = "SecurityPrivacy.SettingsUpdated",
                EntityType = nameof(StoreSecurityPrivacySettings),
                EntityId = settings.PublicId.ToString(),
                Summary = "Security and privacy settings updated.",
                MetadataJson = metadata,
            });
        }

        private static string? Validate(UpdateStoreSecurityPrivacySettingsRequest request)
        {
            if (request.Consent is null)
            {
                return "Consent settings are required.";
            }

            if (request.Captcha is null)
            {
                return "Captcha settings are required.";
            }

            if (request.Privacy is null)
            {
                return "Privacy retention settings are required.";
            }

            if (request.Registration is not null
                && !IsSupportedRegistrationMode(request.Registration.Mode))
            {
                return "Registration mode must be either standard or disabled.";
            }

            if (string.IsNullOrWhiteSpace(request.Consent.CurrentVersion) || request.Consent.CurrentVersion.Length > 64)
            {
                return "Consent version is required and must be 64 characters or fewer.";
            }

            if (string.IsNullOrWhiteSpace(request.Consent.PolicyPagePath) || request.Consent.PolicyPagePath.Length > 256)
            {
                return "Consent policy page path is required and must be 256 characters or fewer.";
            }

            if (string.IsNullOrWhiteSpace(request.Captcha.ProviderSystemName)
                || !ProviderRegex.IsMatch(request.Captcha.ProviderSystemName.Trim()))
            {
                return "Captcha provider system name is invalid.";
            }

            if (request.Captcha.MinimumScore is < 0 or > 1)
            {
                return "Captcha minimum score must be between 0 and 1.";
            }

            return ValidateRange(request.Consent.VisitorCookieLifetimeDays, 1, 3650, "Consent visitor cookie lifetime")
                   ?? ValidateRange(request.Consent.EventRetentionDays, 1, 3650, "Consent event retention")
                   ?? ValidateRange(request.Privacy.RefreshTokenIpRetentionDays, 1, 3650, "Refresh token IP retention")
                   ?? ValidateRange(request.Privacy.RefreshTokenUserAgentRetentionDays, 1, 3650, "Refresh token user-agent retention")
                   ?? ValidateRange(request.Privacy.ConsentEventRetentionDays, 1, 3650, "Consent event retention")
                   ?? ValidateRange(request.Privacy.CaptchaVerificationLogRetentionDays, 1, 3650, "Captcha verification log retention")
                   ?? ValidateRange(request.Privacy.NewsletterConsentEvidenceRetentionDays, 1, 3650, "Newsletter consent evidence retention");
        }

        private static string? ValidateRange(int value, int minimum, int maximum, string label)
        {
            return value < minimum || value > maximum
                ? $"{label} must be between {minimum} and {maximum} days."
                : null;
        }

        private static string GetProviderDisplayName(string providerSystemName)
        {
            return providerSystemName.Trim().ToLowerInvariant() switch
            {
                "recaptcha" => "reCAPTCHA",
                "hcaptcha" => "hCaptcha",
                "turnstile" => "Cloudflare Turnstile",
                "none" => "None",
                var provider => provider,
            };
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizeRegistrationMode(string? value)
        {
            var mode = string.IsNullOrWhiteSpace(value)
                ? RegistrationModeStandard
                : value.Trim().ToLowerInvariant();

            return string.Equals(mode, RegistrationModeDisabled, StringComparison.Ordinal)
                ? RegistrationModeDisabled
                : RegistrationModeStandard;
        }

        private static bool IsSupportedRegistrationMode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var mode = value.Trim().ToLowerInvariant();
            return string.Equals(mode, RegistrationModeStandard, StringComparison.Ordinal)
                   || string.Equals(mode, RegistrationModeDisabled, StringComparison.Ordinal);
        }

        private static ServiceResponse<StoreSecurityPrivacySettingsDto> Success(StoreSecurityPrivacySettingsDto payload, string message)
        {
            return new ServiceResponse<StoreSecurityPrivacySettingsDto>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<StoreSecurityPrivacySettingsDto> Failure(
            string message,
            ServiceResponseType responseType = ServiceResponseType.ValidationError)
        {
            return new ServiceResponse<StoreSecurityPrivacySettingsDto>(false, message)
            {
                ResponseType = responseType,
            };
        }
    }
}
