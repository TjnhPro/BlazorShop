namespace BlazorShop.Storefront.Services
{
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Options;
    using BlazorShop.Storefront.Services.Contracts;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    using GeneratedClients = BlazorShop.Storefront.Client;
    using GeneratedConfigurationClient = BlazorShop.Storefront.Client.IStorefrontConfigurationClient;
    using GeneratedCurrencyClient = BlazorShop.Storefront.Client.IStorefrontCurrencyClient;
    using GeneratedStoreClient = BlazorShop.Storefront.Client.IStorefrontStoreClient;

    public sealed class GeneratedStorefrontConfigurationClient : IStorefrontStoreConfigurationClient
    {
        private const string DefaultStoreStatus = "unavailable";
        private const string DefaultCurrencyCode = "USD";
        private const string DefaultCulture = "en-US";
        private const string CurrencyPreferenceUnavailableMessage = "Unable to update currency preference right now.";

        private readonly GeneratedStoreClient _storeClient;
        private readonly GeneratedConfigurationClient _configurationClient;
        private readonly GeneratedCurrencyClient _currencyClient;
        private readonly IConfiguration _configuration;

        public GeneratedStorefrontConfigurationClient(
            GeneratedStoreClient storeClient,
            GeneratedConfigurationClient configurationClient,
            GeneratedCurrencyClient currencyClient,
            IConfiguration configuration,
            IOptions<StorefrontApiOptions> _)
        {
            _storeClient = storeClient;
            _configurationClient = configurationClient;
            _currencyClient = currencyClient;
            _configuration = configuration;
        }

        public async Task<StorefrontApiResult<StorefrontCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
        {
            var storeKey = ResolveStoreKey();
            if (string.IsNullOrWhiteSpace(storeKey))
            {
                return StorefrontApiResult<StorefrontCurrentStore>.NotFound();
            }

            try
            {
                var response = await _storeClient.GetCurrentAsync(storeKey, cancellationToken);
                return response.Success == true && response.Data is not null
                    ? StorefrontApiResult<StorefrontCurrentStore>.Success(MapCurrentStore(response.Data))
                    : StorefrontApiResult<StorefrontCurrentStore>.ServiceUnavailable();
            }
            catch (GeneratedClients.StorefrontApiException exception) when (exception.StatusCode == StatusCodes.Status404NotFound)
            {
                return StorefrontApiResult<StorefrontCurrentStore>.NotFound();
            }
            catch (Exception exception) when (IsGeneratedClientTransportFailure(exception))
            {
                return StorefrontApiResult<StorefrontCurrentStore>.ServiceUnavailable();
            }
        }

        public async Task<StorefrontApiResult<StorefrontPublicConfiguration>> GetPublicConfigurationAsync(CancellationToken cancellationToken = default)
        {
            var storeKey = ResolveStoreKey();
            if (string.IsNullOrWhiteSpace(storeKey))
            {
                return StorefrontApiResult<StorefrontPublicConfiguration>.NotFound();
            }

            try
            {
                var response = await _configurationClient.GetAsync(storeKey, cancellationToken);
                return response.Success == true && response.Data is not null
                    ? StorefrontApiResult<StorefrontPublicConfiguration>.Success(MapPublicConfiguration(response.Data))
                    : StorefrontApiResult<StorefrontPublicConfiguration>.ServiceUnavailable();
            }
            catch (GeneratedClients.StorefrontApiException exception) when (exception.StatusCode == StatusCodes.Status404NotFound)
            {
                return StorefrontApiResult<StorefrontPublicConfiguration>.NotFound();
            }
            catch (Exception exception) when (IsGeneratedClientTransportFailure(exception))
            {
                return StorefrontApiResult<StorefrontPublicConfiguration>.ServiceUnavailable();
            }
        }

        public async Task<StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>> SetCurrencyPreferenceAsync(
            StorefrontCurrencyPreferenceRequest request,
            CancellationToken cancellationToken = default)
        {
            var storeKey = ResolveStoreKey();
            if (string.IsNullOrWhiteSpace(storeKey))
            {
                return StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>.Failed("Store key is required.");
            }

            try
            {
                var response = await _currencyClient.SetPreferenceAsync(
                    storeKey,
                    new GeneratedClients.StorefrontCurrencyPreferenceRequest
                    {
                        CurrencyCode = request.CurrencyCode,
                    },
                    cancellationToken);
                return response.Success == true && response.Data is not null
                    ? StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>.Succeeded(MapCurrencyPreference(response.Data), response.Message)
                    : StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>.Failed(response.Message);
            }
            catch (GeneratedClients.StorefrontApiException<GeneratedClients.CommerceNodeApiErrorResponse> exception)
            {
                return StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>.Failed(exception.Result.Message);
            }
            catch (Exception exception) when (IsGeneratedClientTransportFailure(exception))
            {
                return StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>.Failed(CurrencyPreferenceUnavailableMessage);
            }
        }

        private string? ResolveStoreKey()
        {
            return StorefrontApiEndpointResolver.ResolveStoreKey(_configuration);
        }

        private static bool IsGeneratedClientTransportFailure(Exception exception)
        {
            return exception is GeneratedClients.StorefrontApiException
                or HttpRequestException
                or TaskCanceledException
                or InvalidOperationException;
        }

        private static StorefrontCurrentStore MapCurrentStore(GeneratedClients.StorefrontCurrentStoreResponse source)
        {
            return new StorefrontCurrentStore(
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

        private static StorefrontPublicConfiguration MapPublicConfiguration(GeneratedClients.StorefrontPublicConfigurationResponse source)
        {
            return new StorefrontPublicConfiguration(
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

        private static StorefrontStoreIdentity MapStoreIdentity(GeneratedClients.StorefrontStoreIdentityResponse? source)
        {
            return new StorefrontStoreIdentity(
                source?.PublicId ?? Guid.Empty,
                NormalizeRequired(source?.StoreKey),
                NormalizeRequired(source?.Name),
                NormalizeRequired(source?.Status, DefaultStoreStatus),
                NormalizeOptional(source?.BaseUrl),
                NormalizeOptional(source?.PrimaryDomain),
                source?.ForceHttps == true);
        }

        private static StorefrontBranding MapBranding(GeneratedClients.StorefrontBrandingResponse? source)
        {
            return new StorefrontBranding(
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

        private static StorefrontLocaleOptions MapLocaleOptions(GeneratedClients.StorefrontLocaleOptionsResponse? source)
        {
            return new StorefrontLocaleOptions(
                NormalizeRequired(source?.DefaultCulture, DefaultCulture),
                NormalizeList(source?.SupportedCultures));
        }

        private static StorefrontCurrencyOptions MapCurrencyOptions(GeneratedClients.StorefrontCurrencyOptionsResponse? source)
        {
            return new StorefrontCurrencyOptions(
                NormalizeRequired(source?.DefaultCurrencyCode, DefaultCurrencyCode),
                NormalizeList(source?.SupportedCurrencyCodes));
        }

        private static StorefrontConsentConfiguration MapConsent(GeneratedClients.StorefrontConsentConfigurationResponse? source)
        {
            return new StorefrontConsentConfiguration(
                source?.Enabled == true,
                source?.BannerRequired == true,
                NormalizeRequired(source?.CurrentVersion),
                NormalizeRequired(source?.PolicyPagePath),
                (source?.Categories ?? []).Select(MapConsentCategory).ToArray(),
                source?.VisitorCookieLifetimeDays ?? 180);
        }

        private static StorefrontConsentCategory MapConsentCategory(GeneratedClients.StorefrontConsentCategoryResponse source)
        {
            return new StorefrontConsentCategory(
                NormalizeRequired(source.Name),
                source.Required == true,
                source.DefaultEnabled == true);
        }

        private static StorefrontCaptchaConfiguration MapCaptcha(GeneratedClients.StorefrontCaptchaConfigurationResponse? source)
        {
            return new StorefrontCaptchaConfiguration(
                source?.Enabled == true,
                NormalizeRequired(source?.ProviderSystemName),
                NormalizeOptional(source?.PublicSiteKey),
                NormalizeList(source?.EnabledTargets),
                new Dictionary<string, string>(source?.ActionNames ?? new Dictionary<string, string>(), StringComparer.Ordinal));
        }

        private static StorefrontMaintenanceState MapMaintenance(GeneratedClients.StorefrontMaintenanceStateResponse? source)
        {
            return new StorefrontMaintenanceState(
                source?.MaintenanceModeEnabled == true,
                NormalizeOptional(source?.MaintenanceMessage));
        }

        private static StorefrontFeatureFlags MapFeatureFlags(GeneratedClients.StorefrontFeatureFlagsResponse? source)
        {
            return new StorefrontFeatureFlags(
                source?.CustomerAccountsEnabled == true,
                source?.CartEnabled == true,
                source?.CheckoutEnabled == true,
                source?.PaymentsEnabled == true,
                source?.NewsletterEnabled == true,
                source?.RecommendationsEnabled == true);
        }

        private static IReadOnlyDictionary<string, StorefrontCapability> MapCapabilities(
            IDictionary<string, GeneratedClients.StorefrontCapabilityResponse>? source)
        {
            return (source ?? new Dictionary<string, GeneratedClients.StorefrontCapabilityResponse>())
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                .ToDictionary(
                    pair => pair.Key.Trim(),
                    pair => new StorefrontCapability(
                        pair.Value.Supported == true,
                        pair.Value.Enabled == true,
                        NormalizeOptional(pair.Value.Reason)),
                    StringComparer.Ordinal);
        }

        private static StorefrontPublicPaymentMethod MapPaymentMethod(GeneratedClients.StorefrontPaymentMethodResponse source)
        {
            return new StorefrontPublicPaymentMethod(
                source.Id ?? Guid.Empty,
                NormalizeRequired(source.Key),
                NormalizeRequired(source.Name),
                NormalizeOptional(source.Description),
                NormalizeOptional(source.ShortDisplayText),
                NormalizeOptional(source.IconUrl),
                NormalizeList(source.SupportedCurrencyCodes),
                NormalizeList(source.SupportedCountryCodes));
        }

        private static StorefrontSeoDefaults MapSeoDefaults(GeneratedClients.StorefrontSeoDefaultsResponse? source)
        {
            return new StorefrontSeoDefaults(
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

        private static StorefrontCurrencyPreferenceResponse MapCurrencyPreference(GeneratedClients.StorefrontCurrencyPreferenceResponse source)
        {
            return new StorefrontCurrencyPreferenceResponse(
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
