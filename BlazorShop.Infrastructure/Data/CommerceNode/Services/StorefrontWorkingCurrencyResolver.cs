namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.RegularExpressions;

    using BlazorShop.Application.CommerceNode.Currencies;

    using Microsoft.EntityFrameworkCore;

    public sealed class StorefrontWorkingCurrencyResolver : IStorefrontWorkingCurrencyResolver
    {
        private static readonly Regex CurrencyCodeRegex = new("^[A-Z]{3}$", RegexOptions.Compiled);

        private readonly CommerceNodeDbContext context;
        private readonly IStoreCurrencyResolver storeCurrencyResolver;

        public StorefrontWorkingCurrencyResolver(
            CommerceNodeDbContext context,
            IStoreCurrencyResolver storeCurrencyResolver)
        {
            this.context = context;
            this.storeCurrencyResolver = storeCurrencyResolver;
        }

        public async Task<StorefrontWorkingCurrencyResolution> ResolveAsync(
            Guid storeId,
            string? requestedCurrencyCode,
            CancellationToken cancellationToken = default)
        {
            var baseCurrencyCode = await this.storeCurrencyResolver.ResolveDefaultCurrencyCodeAsync(storeId, cancellationToken);
            var normalizedRequested = NormalizeCurrencyCode(requestedCurrencyCode);
            if (normalizedRequested is null)
            {
                return new StorefrontWorkingCurrencyResolution(
                    baseCurrencyCode,
                    baseCurrencyCode,
                    null,
                    RequestedCurrencySupported: false,
                    CheckoutCurrencyEnabled: true,
                    Reason: "default");
            }

            var isSupported = string.Equals(normalizedRequested, baseCurrencyCode, StringComparison.Ordinal)
                || await this.context.StoreCurrencies
                    .AsNoTracking()
                    .AnyAsync(
                        currency => currency.StoreId == storeId
                            && currency.CurrencyCode == normalizedRequested
                            && currency.IsEnabled,
                        cancellationToken);

            if (!isSupported)
            {
                return new StorefrontWorkingCurrencyResolution(
                    baseCurrencyCode,
                    baseCurrencyCode,
                    normalizedRequested,
                    RequestedCurrencySupported: false,
                    CheckoutCurrencyEnabled: true,
                    Reason: "unsupported");
            }

            if (string.Equals(normalizedRequested, baseCurrencyCode, StringComparison.Ordinal))
            {
                return new StorefrontWorkingCurrencyResolution(
                    baseCurrencyCode,
                    baseCurrencyCode,
                    normalizedRequested,
                    RequestedCurrencySupported: true,
                    CheckoutCurrencyEnabled: true,
                    Reason: "base");
            }

            return new StorefrontWorkingCurrencyResolution(
                baseCurrencyCode,
                baseCurrencyCode,
                normalizedRequested,
                RequestedCurrencySupported: true,
                CheckoutCurrencyEnabled: false,
                Reason: "conversion_not_enabled");
        }

        private static string? NormalizeCurrencyCode(string? value)
        {
            var normalized = value?.Trim().ToUpperInvariant();
            return normalized is not null && CurrencyCodeRegex.IsMatch(normalized) ? normalized : null;
        }
    }
}
