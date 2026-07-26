namespace BlazorShop.Storefront.Runtime
{
    using BlazorShop.Storefront.Client;

    using GeneratedConfigurationClient = BlazorShop.Storefront.Client.IStorefrontConfigurationClient;
    using GeneratedCurrencyClient = BlazorShop.Storefront.Client.IStorefrontCurrencyClient;
    using GeneratedStoreClient = BlazorShop.Storefront.Client.IStorefrontStoreClient;

    public interface IStorefrontRuntimeConfigurationFacade
    {
        Task<StorefrontRuntimeResult<StorefrontRuntimeCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<StorefrontRuntimePublicConfiguration>> GetPublicConfigurationAsync(CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontRuntimeCurrencyPreferenceResponse>> SetCurrencyPreferenceAsync(
            StorefrontRuntimeCurrencyPreferenceRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed class StorefrontRuntimeConfigurationFacade : IStorefrontRuntimeConfigurationFacade
    {
        private const string DefaultStoreStatus = "unavailable";
        private const string DefaultCurrencyCode = "USD";
        private const string DefaultCulture = "en-US";

        private readonly IStorefrontRuntimeContext context;
        private readonly GeneratedStoreClient storeClient;
        private readonly GeneratedConfigurationClient configurationClient;
        private readonly GeneratedCurrencyClient currencyClient;

        public StorefrontRuntimeConfigurationFacade(
            IStorefrontRuntimeContext context,
            GeneratedStoreClient storeClient,
            GeneratedConfigurationClient configurationClient,
            GeneratedCurrencyClient currencyClient)
        {
            this.context = context;
            this.storeClient = storeClient;
            this.configurationClient = configurationClient;
            this.currencyClient = currencyClient;
        }

        public async Task<StorefrontRuntimeResult<StorefrontRuntimeCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.storeClient.GetCurrentAsync(this.context.RequireStoreKey(), cancellationToken).ConfigureAwait(false);
                return response.Success == true && response.Data is not null
                    ? StorefrontRuntimeResult<StorefrontRuntimeCurrentStore>.Succeeded(MapCurrentStore(response.Data))
                    : StorefrontRuntimeResult<StorefrontRuntimeCurrentStore>.Failed(ServiceUnavailable());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return StorefrontRuntimeResult<StorefrontRuntimeCurrentStore>.Failed(StorefrontRuntimeErrorMapper.FromException(exception));
            }
        }

        public async Task<StorefrontRuntimeResult<StorefrontRuntimePublicConfiguration>> GetPublicConfigurationAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.configurationClient.GetAsync(this.context.RequireStoreKey(), cancellationToken).ConfigureAwait(false);
                return response.Success == true && response.Data is not null
                    ? StorefrontRuntimeResult<StorefrontRuntimePublicConfiguration>.Succeeded(MapPublicConfiguration(response.Data))
                    : StorefrontRuntimeResult<StorefrontRuntimePublicConfiguration>.Failed(ServiceUnavailable());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return StorefrontRuntimeResult<StorefrontRuntimePublicConfiguration>.Failed(StorefrontRuntimeErrorMapper.FromException(exception));
            }
        }

        public async Task<StorefrontRuntimeSubmitResult<StorefrontRuntimeCurrencyPreferenceResponse>> SetCurrencyPreferenceAsync(
            StorefrontRuntimeCurrencyPreferenceRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                var response = await this.currencyClient.SetPreferenceAsync(
                    this.context.RequireStoreKey(),
                    new StorefrontCurrencyPreferenceRequest { CurrencyCode = request.CurrencyCode },
                    cancellationToken).ConfigureAwait(false);
                return response.Success == true && response.Data is not null
                    ? StorefrontRuntimeSubmitResult<StorefrontRuntimeCurrencyPreferenceResponse>.Succeeded(MapCurrencyPreference(response.Data))
                    : StorefrontRuntimeSubmitResult<StorefrontRuntimeCurrencyPreferenceResponse>.Failed(ServiceUnavailable(response.Message));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return StorefrontRuntimeSubmitResult<StorefrontRuntimeCurrencyPreferenceResponse>.Failed(StorefrontRuntimeErrorMapper.FromException(exception));
            }
        }

        private static StorefrontRuntimeError ServiceUnavailable(string? message = null)
        {
            return new StorefrontRuntimeError(
                StorefrontRuntimeStatusCodes.ServiceUnavailable,
                "storefront.unavailable",
                string.IsNullOrWhiteSpace(message) ? "The storefront service is unavailable." : message.Trim(),
                null,
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal));
        }

        private static StorefrontRuntimeCurrentStore MapCurrentStore(StorefrontCurrentStoreResponse source)
        {
            return new StorefrontRuntimeCurrentStore(
                source.PublicId ?? Guid.Empty,
                NormalizeRequired(source.StoreKey),
                NormalizeRequired(source.Name),
                NormalizeRequired(source.Status, DefaultStoreStatus),
                NormalizeOptional(source.BaseUrl),
                NormalizeOptional(source.PrimaryDomain),
                source.ForceHttps == true,
                NormalizeOptional(source.CdnHost),
                NormalizeOptional(source.LogoUrl),
                NormalizeOptional(source.CompanyName),
                NormalizeOptional(source.CompanyEmail),
                NormalizeOptional(source.CompanyPhone),
                NormalizeOptional(source.CompanyAddress),
                NormalizeOptional(source.FaviconUrl),
                NormalizeOptional(source.PngIconUrl),
                NormalizeOptional(source.AppleTouchIconUrl),
                NormalizeOptional(source.MsTileImageUrl),
                NormalizeOptional(source.MsTileColor),
                NormalizeRequired(source.DefaultCurrencyCode, DefaultCurrencyCode),
                NormalizeRequired(source.DefaultCulture, DefaultCulture),
                NormalizeOptional(source.SupportEmail),
                NormalizeOptional(source.SupportPhone),
                source.MaintenanceModeEnabled == true,
                NormalizeOptional(source.MaintenanceMessage),
                NormalizeOptional(source.HtmlBodyId));
        }

        private static StorefrontRuntimePublicConfiguration MapPublicConfiguration(StorefrontPublicConfigurationResponse source)
        {
            return new StorefrontRuntimePublicConfiguration(
                MapStoreIdentity(source.StoreIdentity),
                MapBranding(source.Branding),
                MapLocaleOptions(source.LocaleOptions),
                MapCurrencyOptions(source.CurrencyOptions),
                MapConsent(source.Consent),
                MapCaptcha(source.Captcha),
                MapMaintenance(source.MaintenanceState),
                MapFeatureFlags(source.FeatureFlags),
                MapCapabilities(source.Features),
                (source.PaymentMethods ?? []).Select(MapPaymentMethod).ToArray(),
                MapSeoDefaults(source.SeoDefaults));
        }

        private static StorefrontRuntimeStoreIdentity MapStoreIdentity(StorefrontStoreIdentityResponse? source)
        {
            return new StorefrontRuntimeStoreIdentity(
                source?.PublicId ?? Guid.Empty,
                NormalizeRequired(source?.StoreKey),
                NormalizeRequired(source?.Name),
                NormalizeRequired(source?.Status, DefaultStoreStatus),
                NormalizeOptional(source?.BaseUrl),
                NormalizeOptional(source?.PrimaryDomain),
                source?.ForceHttps == true);
        }

        private static StorefrontRuntimeBranding MapBranding(StorefrontBrandingResponse? source)
        {
            return new StorefrontRuntimeBranding(
                NormalizeOptional(source?.CdnHost),
                NormalizeOptional(source?.LogoUrl),
                NormalizeOptional(source?.CompanyName),
                NormalizeOptional(source?.CompanyEmail),
                NormalizeOptional(source?.CompanyPhone),
                NormalizeOptional(source?.CompanyAddress),
                NormalizeOptional(source?.FaviconUrl),
                NormalizeOptional(source?.PngIconUrl),
                NormalizeOptional(source?.AppleTouchIconUrl),
                NormalizeOptional(source?.MsTileImageUrl),
                NormalizeOptional(source?.MsTileColor),
                NormalizeOptional(source?.SupportEmail),
                NormalizeOptional(source?.SupportPhone),
                NormalizeOptional(source?.HtmlBodyId));
        }

        private static StorefrontRuntimeLocaleOptions MapLocaleOptions(StorefrontLocaleOptionsResponse? source)
        {
            return new StorefrontRuntimeLocaleOptions(
                NormalizeRequired(source?.DefaultCulture, DefaultCulture),
                NormalizeList(source?.SupportedCultures));
        }

        private static StorefrontRuntimeCurrencyOptions MapCurrencyOptions(StorefrontCurrencyOptionsResponse? source)
        {
            return new StorefrontRuntimeCurrencyOptions(
                NormalizeRequired(source?.DefaultCurrencyCode, DefaultCurrencyCode),
                NormalizeList(source?.SupportedCurrencyCodes));
        }

        private static StorefrontRuntimeConsentConfiguration MapConsent(StorefrontConsentConfigurationResponse? source)
        {
            return new StorefrontRuntimeConsentConfiguration(
                source?.Enabled == true,
                source?.BannerRequired == true,
                NormalizeRequired(source?.CurrentVersion),
                NormalizeRequired(source?.PolicyPagePath),
                (source?.Categories ?? []).Select(MapConsentCategory).ToArray(),
                source?.VisitorCookieLifetimeDays ?? 180);
        }

        private static StorefrontRuntimeConsentCategory MapConsentCategory(StorefrontConsentCategoryResponse source)
        {
            return new StorefrontRuntimeConsentCategory(
                NormalizeRequired(source.Name),
                source.Required == true,
                source.DefaultEnabled == true);
        }

        private static StorefrontRuntimeCaptchaConfiguration MapCaptcha(StorefrontCaptchaConfigurationResponse? source)
        {
            return new StorefrontRuntimeCaptchaConfiguration(
                source?.Enabled == true,
                NormalizeRequired(source?.ProviderSystemName),
                NormalizeOptional(source?.PublicSiteKey),
                NormalizeList(source?.EnabledTargets),
                new Dictionary<string, string>(source?.ActionNames ?? new Dictionary<string, string>(), StringComparer.Ordinal));
        }

        private static StorefrontRuntimeMaintenanceState MapMaintenance(StorefrontMaintenanceStateResponse? source)
        {
            return new StorefrontRuntimeMaintenanceState(
                source?.MaintenanceModeEnabled == true,
                NormalizeOptional(source?.MaintenanceMessage));
        }

        private static StorefrontRuntimeFeatureFlags MapFeatureFlags(StorefrontFeatureFlagsResponse? source)
        {
            return new StorefrontRuntimeFeatureFlags(
                source?.CustomerAccountsEnabled == true,
                source?.CartEnabled == true,
                source?.CheckoutEnabled == true,
                source?.PaymentsEnabled == true,
                source?.NewsletterEnabled == true,
                source?.RecommendationsEnabled == true);
        }

        private static IReadOnlyDictionary<string, StorefrontRuntimeCapabilityState> MapCapabilities(
            IDictionary<string, StorefrontCapabilityResponse>? source)
        {
            return (source ?? new Dictionary<string, StorefrontCapabilityResponse>())
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                .ToDictionary(
                    pair => pair.Key.Trim(),
                    pair => new StorefrontRuntimeCapabilityState(
                        pair.Value.Supported == true,
                        pair.Value.Enabled == true,
                        NormalizeOptional(pair.Value.Reason)),
                    StringComparer.Ordinal);
        }

        private static StorefrontRuntimePublicPaymentMethod MapPaymentMethod(StorefrontPaymentMethodResponse source)
        {
            return new StorefrontRuntimePublicPaymentMethod(
                source.Id ?? Guid.Empty,
                NormalizeRequired(source.Key),
                NormalizeRequired(source.Name),
                NormalizeOptional(source.Description),
                NormalizeOptional(source.ShortDisplayText),
                NormalizeOptional(source.IconUrl),
                NormalizeList(source.SupportedCurrencyCodes),
                NormalizeList(source.SupportedCountryCodes));
        }

        private static StorefrontRuntimeSeoDefaults MapSeoDefaults(StorefrontSeoDefaultsResponse? source)
        {
            return new StorefrontRuntimeSeoDefaults(
                NormalizeOptional(source?.SiteName),
                NormalizeOptional(source?.DefaultTitleSuffix),
                NormalizeOptional(source?.DefaultMetaDescription),
                NormalizeOptional(source?.DefaultOgImage),
                NormalizeOptional(source?.BaseCanonicalUrl),
                NormalizeOptional(source?.CompanyName),
                NormalizeOptional(source?.CompanyLogoUrl),
                NormalizeOptional(source?.CompanyPhone),
                NormalizeOptional(source?.CompanyEmail),
                NormalizeOptional(source?.CompanyAddress),
                NormalizeOptional(source?.FacebookUrl),
                NormalizeOptional(source?.InstagramUrl),
                NormalizeOptional(source?.XUrl));
        }

        private static StorefrontRuntimeCurrencyPreferenceResponse MapCurrencyPreference(StorefrontCurrencyPreferenceResponse source)
        {
            return new StorefrontRuntimeCurrencyPreferenceResponse(
                NormalizeRequired(source.CurrencyCode, DefaultCurrencyCode),
                NormalizeRequired(source.BaseCurrencyCode, DefaultCurrencyCode),
                NormalizeOptional(source.RequestedCurrencyCode),
                source.RequestedCurrencySupported == true,
                source.CheckoutCurrencyEnabled == true,
                NormalizeRequired(source.Reason));
        }

        private static IReadOnlyList<string> NormalizeList(IEnumerable<string>? values)
        {
            return (values ?? [])
                .Select(NormalizeOptional)
                .Where(value => value is not null)
                .Select(value => value!)
                .ToArray();
        }

        private static string NormalizeRequired(string? value, string fallback = "")
        {
            return NormalizeOptional(value) ?? fallback;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
