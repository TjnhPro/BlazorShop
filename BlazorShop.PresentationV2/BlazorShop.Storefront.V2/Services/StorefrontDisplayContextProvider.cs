namespace BlazorShop.Storefront.Services
{
    using System.Globalization;

    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2;

    using Microsoft.AspNetCore.Http;

    public sealed class StorefrontDisplayContextProvider : IStorefrontDisplayContextProvider
    {
        private const string DefaultCultureName = "en-US";
        private const string DefaultLanguageCode = "en";
        private const string DefaultCurrencyCode = "USD";

        private readonly IStorefrontCurrentStoreProvider _currentStoreProvider;
        private readonly StorefrontApiClient? _apiClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StorefrontDisplayContextProvider(
            IStorefrontCurrentStoreProvider currentStoreProvider,
            StorefrontApiClient? apiClient = null,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _currentStoreProvider = currentStoreProvider;
            _apiClient = apiClient;
            _httpContextAccessor = httpContextAccessor ?? new HttpContextAccessor();
        }

        public async Task<StorefrontDisplayContext> GetAsync(CancellationToken cancellationToken = default)
        {
            var resolution = await _currentStoreProvider.ResolveAsync(cancellationToken);
            var store = resolution.Store;
            if (store is null)
            {
                return StorefrontDisplayContext.Fallback;
            }

            var culture = ResolveCulture(store.DefaultCulture);
            var defaultCurrencyCode = NormalizeCurrency(store.DefaultCurrencyCode);
            var supportedCurrencyCodes = await ResolveSupportedCurrencyCodesAsync(defaultCurrencyCode, cancellationToken);
            var requestedCurrencyCode = NormalizeCurrencyOrNull(
                _httpContextAccessor.HttpContext?.Request.Cookies[StorefrontCookieNames.CurrencyPreference]);
            var currencyCode = requestedCurrencyCode is not null
                && supportedCurrencyCodes.Contains(requestedCurrencyCode, StringComparer.Ordinal)
                    ? requestedCurrencyCode
                    : defaultCurrencyCode;
            var storeName = string.IsNullOrWhiteSpace(store.Name) ? StorefrontDisplayContext.Fallback.StoreName : store.Name.Trim();

            return new StorefrontDisplayContext(
                string.IsNullOrWhiteSpace(store.StoreKey) ? StorefrontDisplayContext.Fallback.StoreKey : store.StoreKey.Trim(),
                storeName,
                culture.Name,
                string.IsNullOrWhiteSpace(culture.TwoLetterISOLanguageName) ? DefaultLanguageCode : culture.TwoLetterISOLanguageName,
                currencyCode,
                NormalizeOptional(store.LogoUrl),
                NormalizeOptional(store.FaviconUrl),
                NormalizeOptional(store.PngIconUrl),
                NormalizeOptional(store.AppleTouchIconUrl),
                NormalizeOptional(store.MsTileImageUrl),
                NormalizeOptional(store.MsTileColor),
                NormalizeOptional(store.CompanyName),
                NormalizeOptional(store.CompanyEmail),
                NormalizeOptional(store.CompanyPhone),
                NormalizeOptional(store.CompanyAddress),
                NormalizeOptional(store.SupportEmail),
                NormalizeOptional(store.SupportPhone),
                defaultCurrencyCode,
                supportedCurrencyCodes);
        }

        private async Task<IReadOnlyList<string>> ResolveSupportedCurrencyCodesAsync(
            string defaultCurrencyCode,
            CancellationToken cancellationToken)
        {
            if (_apiClient is null)
            {
                return [defaultCurrencyCode];
            }

            var result = await _apiClient.GetPublicConfigurationAsync(cancellationToken);
            var configuredCodes = result.IsSuccess
                ? result.Value?.CurrencyOptions.SupportedCurrencyCodes
                : null;

            var codes = (configuredCodes ?? [])
                .Prepend(defaultCurrencyCode)
                .Select(NormalizeCurrencyOrNull)
                .Where(code => code is not null)
                .Select(code => code!)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            return codes.Length == 0 ? [defaultCurrencyCode] : codes;
        }

        private static CultureInfo ResolveCulture(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                try
                {
                    return CultureInfo.GetCultureInfo(value.Trim());
                }
                catch (CultureNotFoundException)
                {
                    // Fall through to the stable Storefront default.
                }
            }

            return CultureInfo.GetCultureInfo(DefaultCultureName);
        }

        private static string NormalizeCurrency(string? value)
        {
            return NormalizeCurrencyOrNull(value) ?? DefaultCurrencyCode;
        }

        private static string? NormalizeCurrencyOrNull(string? value)
        {
            var normalized = NormalizeOptional(value)?.ToUpperInvariant();
            return normalized is { Length: 3 } && normalized.All(char.IsLetter)
                ? normalized
                : null;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
