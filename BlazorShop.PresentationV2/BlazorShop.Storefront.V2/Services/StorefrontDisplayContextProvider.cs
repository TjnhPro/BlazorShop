namespace BlazorShop.Storefront.Services
{
    using System.Globalization;

    using BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontDisplayContextProvider : IStorefrontDisplayContextProvider
    {
        private const string DefaultCultureName = "en-US";
        private const string DefaultLanguageCode = "en";
        private const string DefaultCurrencyCode = "USD";

        private readonly IStorefrontCurrentStoreProvider _currentStoreProvider;

        public StorefrontDisplayContextProvider(IStorefrontCurrentStoreProvider currentStoreProvider)
        {
            _currentStoreProvider = currentStoreProvider;
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
            var currencyCode = NormalizeCurrency(store.DefaultCurrencyCode);
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
                NormalizeOptional(store.SupportPhone));
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
            var normalized = NormalizeOptional(value)?.ToUpperInvariant();
            return normalized is { Length: 3 } && normalized.All(char.IsLetter)
                ? normalized
                : DefaultCurrencyCode;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
