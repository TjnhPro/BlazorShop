namespace BlazorShop.Storefront.Services
{
    using System.Globalization;

    using BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontPriceFormatter : IStorefrontPriceFormatter
    {
        public string Format(decimal amount, StorefrontDisplayContext displayContext)
        {
            ArgumentNullException.ThrowIfNull(displayContext);

            var culture = ResolveCulture(displayContext.CultureName);
            var currencyCode = NormalizeCurrency(displayContext.CurrencyCode);
            return $"{currencyCode} {amount.ToString("N2", culture)}";
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
                    // Fall through to invariant formatting.
                }
            }

            return CultureInfo.GetCultureInfo("en-US");
        }

        private static string NormalizeCurrency(string? value)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } && normalized.All(char.IsLetter)
                ? normalized
                : "USD";
        }
    }
}
